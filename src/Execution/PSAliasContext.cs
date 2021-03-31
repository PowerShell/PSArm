
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Primitive;
using PSArm.Commands.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PSArm.Execution
{
    internal sealed class PSAliasContext : IDisposable
    {
        private static readonly Func<SessionState, List<Dictionary<string, AliasInfo>>> s_getAliasTable;

        private static readonly Action<SessionState, AliasInfo, string> s_setAlias;

        private static readonly Action<SessionState, string> s_removeAlias;

        private static readonly HashSet<string> s_psArmAliases = new HashSet<string>(new[]
        {
            NewPSArmTemplateCommand.KeywordName,
            NewPSArmArrayCommand.KeywordName,
            NewPSArmElementCommand.KeywordName,
            NewPSArmSkuCommand.KeywordName,
            NewPSArmDependsOnCommand.KeywordName,
            NewPSArmOutputCommand.KeywordName,
            NewPSArmFunctionCallCommand.KeywordName,
            NewPSArmEntryCommand.KeywordName,
            NewPSArmResourceCommand.KeywordName,
        });

        static PSAliasContext()
        {
            // Our choices for alias manipulation are:
            //  - Call the cmdlets for each alias (and lose scope info needed for restore)
            //  - Use the provider for each alias (and lose scope info needed for restore)
            //  - Use reflection to run internal engine methods
            //  - Use reflection, but compile it to make it more efficient at the cost of readability
            //
            // Since we want to restore aliases exactly as we found them,
            // and also may be running an arbitrary number of ARM templates in a session,
            // we choose the last option.

            PropertyInfo ssInternalProperty = typeof(SessionState)
                .GetProperty("Internal", BindingFlags.NonPublic | BindingFlags.Instance);

            Type ssInternalType = ssInternalProperty.PropertyType;
            MethodInfo ssInternalGetter = ssInternalProperty.GetGetMethod(nonPublic: true);

            s_getAliasTable = GenerateGetAliasTableFunction(ssInternalType, ssInternalGetter);
            s_setAlias = GenerateSetAliasFunction(ssInternalType, ssInternalGetter);
            s_removeAlias = GenerateRemoveAliasFunction(ssInternalType, ssInternalGetter);
        }

        public static PSAliasContext EnterCleanAliasContext(SessionState sessionState)
        {
            List<Dictionary<string, AliasInfo>> aliasTable = EnterCleanScope(sessionState);
            return new PSAliasContext(sessionState, aliasTable);
        }

        private readonly SessionState _sessionState;
        private readonly List<Dictionary<string, AliasInfo>> _aliasTable;

        private PSAliasContext(SessionState sessionState, List<Dictionary<string, AliasInfo>> aliasTable)
        {
            _sessionState = sessionState;
            _aliasTable = aliasTable;
        }

        public void Dispose()
        {
            RestoreOldScope(_sessionState, _aliasTable);
        }

        private static List<Dictionary<string, AliasInfo>> EnterCleanScope(SessionState sessionState)
        {
            List<Dictionary<string, AliasInfo>> aliasTable = s_getAliasTable(sessionState);

            foreach (Dictionary<string, AliasInfo> scope in aliasTable)
            {
                foreach (string alias in scope.Keys)
                {
                    if (!s_psArmAliases.Contains(alias))
                    {
                        s_removeAlias(sessionState, alias);
                    }
                }
            }

            return aliasTable;
        }

        private static void RestoreOldScope(SessionState sessionState, List<Dictionary<string, AliasInfo>> aliasTable)
        {
            // Traverse the alias table from highest scope to lowest
            aliasTable.Reverse();
            for (int i = 0; i < aliasTable.Count; i++)
            {
                foreach (KeyValuePair<string, AliasInfo> alias in aliasTable[i])
                {
                    s_setAlias(sessionState, alias.Value, i.ToString());
                }
            }
        }

        private static Func<SessionState, List<Dictionary<string, AliasInfo>>> GenerateGetAliasTableFunction(
            Type ssInternalType,
            MethodInfo ssInternalGetter)
        {
            // This field got renamed at some point since PS 5.1 -- for now we assume we're safe with the Framework/Core condition
#if CoreCLR
            FieldInfo ssInternalCurrentScopeField = ssInternalType.GetField("_currentScope", BindingFlags.NonPublic | BindingFlags.Instance);
#else
            FieldInfo ssInternalCurrentScopeField = ssInternalType.GetField("currentScope", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
            Type scopeType = ssInternalCurrentScopeField.FieldType;
            ConstructorInfo scopeEnumeratorConstructor = ssInternalType.Assembly.GetType("System.Management.Automation.SessionStateScopeEnumerator")
                .GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    binder: null,
                    new Type[] { scopeType },
                    modifiers: null);
            MethodInfo scopeGetAliasesMethod = scopeType.GetMethod(
                "GetAliases",
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                Array.Empty<Type>(),
                modifiers: null);
            MethodInfo aggregateMethod = typeof(PSAliasContext).GetMethod(
                nameof(PSAliasContext.Aggregate),
                BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(scopeType, typeof(Dictionary<string, AliasInfo>));
            ConstructorInfo dictionaryConstructor = typeof(Dictionary<string, AliasInfo>)
                .GetConstructor(new[] { typeof(Dictionary<string, AliasInfo>) });

            var ssParameter = Expression.Parameter(typeof(SessionState));
            var scopeParameter = Expression.Parameter(scopeType);

            // We want to generate code like:
            //
            //   Aggregate(new ScopeEnumerator(SessionState.Internal._currentScope), (scope) => new Dictionary<string, AliasInfo>(scope.GetAliases()))

            return Expression.Lambda<Func<SessionState, List<Dictionary<string, AliasInfo>>>>(
                Expression.Call(
                    aggregateMethod,
                    Expression.New(
                        scopeEnumeratorConstructor,
                        Expression.Field(
                            Expression.Call(
                                ssParameter,
                                ssInternalGetter),
                            ssInternalCurrentScopeField)),
                    Expression.Lambda(
                        Expression.New(
                            dictionaryConstructor,
                            Expression.Call(
                                scopeParameter,
                                scopeGetAliasesMethod)),
                        scopeParameter)),
                ssParameter).Compile();
        }

        private static Action<SessionState, AliasInfo, string> GenerateSetAliasFunction(
            Type ssInternalType,
            MethodInfo ssInternalGetter)
        {
            MethodInfo ssInternalSetAliasItemAtScopeMethod = ssInternalType.GetMethod(
                "SetAliasItemAtScope",
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                new[] { typeof(AliasInfo), typeof(string), typeof(bool), typeof(CommandOrigin) },
                modifiers: null);

            var paramSessionState = Expression.Parameter(typeof(SessionState));
            var paramAliasInfo = Expression.Parameter(typeof(AliasInfo));
            var paramScopeName = Expression.Parameter(typeof(string));

            // Generate code like:
            //
            //   SessionState.Internal.SetAliasItemAtScope(alias, scope, force: true, CommandOrigin.Internal)

            return Expression.Lambda<Action<SessionState, AliasInfo, string>>(
                Expression.Call(
                    Expression.Call(
                        paramSessionState,
                        ssInternalGetter),
                    ssInternalSetAliasItemAtScopeMethod,
                    paramAliasInfo,
                    paramScopeName,
                    Expression.Constant(true),
                    Expression.Constant(CommandOrigin.Internal)),
                paramSessionState,
                paramAliasInfo,
                paramScopeName).Compile();
        }

        private static Action<SessionState, string> GenerateRemoveAliasFunction(
            Type ssInternalType,
            MethodInfo ssInternalGetter)
        {
            MethodInfo ssInternalRemoveAliasMethod = ssInternalType.GetMethod(
                "RemoveAlias",
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                new[] { typeof(string), typeof(bool) },
                modifiers: null);

            var paramSessionState = Expression.Parameter(typeof(SessionState));
            var paramAliasName = Expression.Parameter(typeof(string));

            // Generate code like:
            //
            //   SessionState.Internal.RemoveAlias(aliasName, force: true)

            return Expression.Lambda<Action<SessionState, string>>(
                Expression.Call(
                    Expression.Call(
                        paramSessionState,
                        ssInternalGetter),
                    ssInternalRemoveAliasMethod,
                    paramAliasName,
                    Expression.Constant(true)),
                paramSessionState,
                paramAliasName).Compile();
        }

        private static List<S> Aggregate<T, S>(IEnumerable<T> enumerable, Func<T, S> func)
        {
            var list = new List<S>();
            foreach (T item in enumerable)
            {
                list.Add(func(item));
            }
            return list;
        }
    }
}

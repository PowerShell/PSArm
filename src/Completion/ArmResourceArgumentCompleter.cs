
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using PSArm.Commands.Template;
using PSArm.Internal;
using PSArm.Schema;
using PSArm.Templates.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PSArm.Completion
{
    /// <summary>
    /// Argument completer for cmdlet arguments that take an ARM resource type,
    /// such as 'Microsoft.Network/publicIpAddresses'.
    /// </summary>
    public class ArmResourceArgumentCompleter : IArgumentCompleter
    {
        private static string[] s_locations = new []
        {
            "WestUS",
            "WestUS2",
            "CentralUS"
        };

        /// <summary>
        /// Complete an ARM resource "type" argument.
        /// </summary>
        /// <param name="commandName">The name of the invoked command whose argument this is.</param>
        /// <param name="parameterName">The name of the parameter for the argument, if any.</param>
        /// <param name="wordToComplete">The argument so far.</param>
        /// <param name="commandAst">The entirety of command AST in which this argument is being provided.</param>
        /// <param name="fakeBoundParameters">The attempted parameter binding, for providing something similar to $PSBoundParameters.</param>
        /// <returns></returns>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            (string provider, string type, string apiVersion) = GetConstrainingParameters(fakeBoundParameters);

            if (IsString(parameterName, nameof(NewPSArmResourceCommand.Provider)))
            {
                var providerCompletions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ResourceSchema resource in ResourceIndex.SharedInstance.GetResourceSchemas())
                {
                    if (apiVersion is not null
                        && !resource.ApiVersion.StartsWith(apiVersion))
                    {
                        continue;
                    }

                    if (type is not null
                        && !resource.Name.StartsWith(type))
                    {
                        continue;
                    }

                    if (wordToComplete is not null
                        && !resource.Namespace.StartsWith(wordToComplete))
                    {
                        continue;
                    }

                    providerCompletions.Add(resource.Namespace);
                }

                return GetCompletionResultsFromStrings(providerCompletions);
            }

            if (IsString(parameterName, nameof(NewPSArmResourceCommand.Type)))
            {
                var providerCompletions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ResourceSchema resource in ResourceIndex.SharedInstance.GetResourceSchemas())
                {
                    if (apiVersion is not null
                        && !resource.ApiVersion.StartsWith(apiVersion))
                    {
                        continue;
                    }

                    if (provider is not null
                        && !resource.Namespace.StartsWith(provider))
                    {
                        continue;
                    }

                    if (wordToComplete is not null
                        && !resource.Name.StartsWith(wordToComplete))
                    {
                        continue;
                    }

                    providerCompletions.Add(resource.Name);
                }

                return GetCompletionResultsFromStrings(providerCompletions);
            }

            if (IsString(parameterName, nameof(NewPSArmResourceCommand.ApiVersion)))
            {
                var providerCompletions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ResourceSchema resource in ResourceIndex.SharedInstance.GetResourceSchemas())
                {
                    if (provider is not null
                        && !resource.Namespace.StartsWith(provider))
                    {
                        continue;
                    }

                    if (type is not null
                        && !resource.Name.StartsWith(type))
                    {
                        continue;
                    }

                    if (wordToComplete is not null
                        && !resource.ApiVersion.StartsWith(wordToComplete))
                    {
                        continue;
                    }

                    providerCompletions.Add(resource.ApiVersion);
                }

                return GetCompletionResultsFromStrings(providerCompletions);
            }

            return null;
        }

        private (string provider, string type, string apiVersion) GetConstrainingParameters(IDictionary fakeBoundParameters)
        {
            string provider = null;
            string type = null;
            string apiVersion = null;

            if (fakeBoundParameters.TryGetValue(nameof(NewPSArmResourceCommand.Provider), out IArmString providerArmStr)
                && providerArmStr is ArmStringLiteral providerLiteral)
            {
                provider = providerLiteral.Value;
            }

            if (fakeBoundParameters.TryGetValue(nameof(NewPSArmResourceCommand.Type), out IArmString typeArmStr)
                && typeArmStr is ArmStringLiteral typeLiteral)
            {
                type = typeLiteral.Value;
            }

            if (fakeBoundParameters.TryGetValue(nameof(NewPSArmResourceCommand.ApiVersion), out IArmString apiVersionArmStr)
                && apiVersionArmStr is ArmStringLiteral apiVersionLiteral)
            {
                apiVersion = apiVersionLiteral.Value;
            }

            return (provider, type, apiVersion);
        }

        private static IEnumerable<CompletionResult> GetCompletionResultsFromStrings(IReadOnlyCollection<string> stringValues)
        {
            var completions = new CompletionResult[stringValues.Count];
            int i = 0;
            foreach (string str in stringValues)
            {
                completions[i] = new CompletionResult(str, str, CompletionResultType.ParameterValue, str);
            }
            return completions;
        }

        private static bool IsString(string str, string expected)
        {
            return string.Equals(str, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
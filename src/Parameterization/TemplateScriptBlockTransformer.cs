
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm.Parameterization
{
    internal class TemplateScriptBlockTransformer
    {
        private readonly PowerShell _pwsh;

        public TemplateScriptBlockTransformer(PowerShell pwsh)
        {
            _pwsh = pwsh;
        }

        public ScriptBlock GetDeparameterizedTemplateScriptBlock(
            ScriptBlock scriptBlock,
            out ArmObject<ArmParameter> armParameters,
            out ArmObject<ArmVariable> armVariables,
            out object[] psArgsArray)
        {
            var ast = (ScriptBlockAst)scriptBlock.Ast;

            if (ast.ParamBlock?.Parameters is null)
            {
                armParameters = null;
                armVariables = null;
                psArgsArray = null;
                return scriptBlock;
            }

            (HashSet<string> parameterAsts, HashSet<string> variableAsts, Dictionary<string, int> argsIndex) = CollectParametersToTransform(ast.ParamBlock.Parameters);

            // We need to evaluate parameters first
            var parametersWithAllowedValues = new HashSet<string>();
            armParameters = new PowerShellArmParameterConstructor(parameterAsts, parametersWithAllowedValues)
                .ConstructParameters(ast.ParamBlock);

            // Construct the list of parameter values to supply for variables
            var parameterVariables = new List<PSVariable>(armParameters.Count);
            foreach (ArmParameter armParameter in armParameters.Values)
            {
                var psVar = new PSVariable(armParameter.Name.CoerceToString(), armParameter.GetReference());
                parameterVariables.Add(psVar);
            }

            // Now do variables
            armVariables = new PowerShellArmVariableConstructor(variableAsts, parameterVariables)
                .ConstructParameters(ast.ParamBlock);

            psArgsArray = BuildInvocationArgumentArray(argsIndex, armParameters, armVariables);

            // If no ASTs need to be recreated, we can use the old one (and prefer to)
            if (parametersWithAllowedValues.Count == 0)
            {
                return scriptBlock;
            }

            return ConstructScriptBlockWithNewParamBlock(ast, parametersWithAllowedValues);
        }

        private object[] BuildInvocationArgumentArray(
            Dictionary<string, int> argumentIndex,
            IReadOnlyDictionary<IArmString, ArmParameter> armParameters,
            IReadOnlyDictionary<IArmString, ArmVariable> armVariables)
        {
            var argsArray = new object[argumentIndex.Count];

            foreach (KeyValuePair<IArmString, ArmParameter> armParameter in armParameters)
            {
                int index = argumentIndex[armParameter.Key.CoerceToString()];
                argsArray[index] = armParameter.Value;
            }

            foreach (KeyValuePair<IArmString, ArmVariable> armVariable in armVariables)
            {
                int index = argumentIndex[armVariable.Key.CoerceToString()];
                argsArray[index] = armVariable.Value;
            }

            return argsArray;
        }

        private ScriptBlock ConstructScriptBlockWithNewParamBlock(ScriptBlockAst oldScriptBlockAst, HashSet<string> parametersToRedefine)
        {
            var parameterAsts = new List<ParameterAst>(oldScriptBlockAst.ParamBlock.Parameters.Count);

            foreach (ParameterAst parameter in oldScriptBlockAst.ParamBlock.Parameters)
            {
                if (!parametersToRedefine.Contains(parameter.Name.VariablePath.UserPath))
                {
                    parameterAsts.Add(parameter);
                    continue;
                }

                parameterAsts.Add(CreateStrippedParameterAst(parameter));
            }

            var newParamBlock = new ParamBlockAst(
                oldScriptBlockAst.ParamBlock.Extent,
                CopyAstCollection(oldScriptBlockAst.Attributes),
                CopyAstCollection(parameterAsts));

            return new ScriptBlockAst(
                oldScriptBlockAst.Extent,
                newParamBlock,
                (NamedBlockAst)oldScriptBlockAst.BeginBlock?.Copy(),
                (NamedBlockAst)oldScriptBlockAst.ProcessBlock?.Copy(),
                (NamedBlockAst)oldScriptBlockAst.EndBlock?.Copy(),
                (NamedBlockAst)oldScriptBlockAst.DynamicParamBlock?.Copy()).GetScriptBlock();
        }

        private ParameterAst CreateStrippedParameterAst(ParameterAst parameter)
        {
            return new ParameterAst(
                parameter.Extent,
                (VariableExpressionAst)parameter.Name.Copy(),
                Enumerable.Empty<AttributeAst>(),
                defaultValue: null);
        }

        private (HashSet<string> parameters, HashSet<string> variables, Dictionary<string, int> argsIndex) CollectParametersToTransform(IEnumerable<ParameterAst> parameterAsts)
        {
            var parameters = new HashSet<string>();
            var variables = new HashSet<string>();
            var argsIndex = new Dictionary<string, int>();
            int i = 0;

            foreach (ParameterAst parameterAst in parameterAsts)
            {
                string paramName = parameterAst.Name.VariablePath.UserPath;
                switch (PSArmParameterization.GetPSArmVarType(parameterAst))
                {
                    case PSArmVarType.Parameter:
                        parameters.Add(paramName);
                        break;

                    case PSArmVarType.Variable:
                        variables.Add(paramName);
                        break;

                    default:
                        throw new ArgumentException($"Parameter '{parameterAst.Name}' must either be of type 'ArmVariable' or 'ArmParameter`1'");
                }

                argsIndex[paramName] = i;
                i++;
            }

            return (parameters, variables, argsIndex);
        }

        private List<TAst> CopyAstCollection<TAst>(IReadOnlyCollection<TAst> asts) where TAst : Ast
        {
            if (asts is null)
            {
                return null;
            }

            var list = new List<TAst>(asts.Count);
            foreach (TAst ast in asts)
            {
                list.Add((TAst)ast.Copy());
            }

            return list;
        }
    }
}

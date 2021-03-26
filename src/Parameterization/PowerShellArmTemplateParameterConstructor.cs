
// Copyright (c) Microsoft Corporation.

using PSArm.Templates;
using PSArm.Templates.Primitives;
using PSArm.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm.Parameterization
{
    internal abstract class PowerShellArmTemplateParameterConstructor
        <TArmParameter> : TemplateParameterConstructor<TArmParameter, ParamBlockAst, ParameterAst, string, List<PSVariable>>
        where TArmParameter : ArmElement, IArmReferenceable
    {

        private readonly HashSet<string> _parameterNames;

        public PowerShellArmTemplateParameterConstructor(
            HashSet<string> parameterNames)
        {
            _parameterNames = parameterNames;
        }

        protected override List<PSVariable> CreateEvaluationState()
        {
            return new List<PSVariable>(_parameterNames.Count);
        }

        protected ArmElement GetParameterValue(ParameterAst parameter, List<PSVariable> variables)
        {
            if (parameter.DefaultValue is null)
            {
                return null;
            }

            // We need to supply the predefined variables here, so must use the ScriptBlock.InvokeWithContext() method
            // This relies on this method being executed on the pipeline thread
            // So any attempt to make this asynchronous or parallelize it will lead to subtle bugs
            Collection<PSObject> result = ScriptBlock
                .Create(parameter.DefaultValue.Extent.Text)
                .InvokeWithContext(functionsToDefine: null, variables);

            object input = result.Count == 1 ? result[0] : result;

            // Define the variable for use in the next parameter evaluation
            variables.Add(new PSVariable(parameter.Name.VariablePath.UserPath, input));

            if (!ArmElementConversion.TryConvertToArmElement(input, out ArmElement armElement))
            {
                throw new ArgumentException($"Unable to convert value '{parameter.DefaultValue}' to ARM value");
            }

            return armElement;
        }

        protected override IReadOnlyDictionary<ParameterAst, IReadOnlyList<string>> CollectReferences(ParamBlockAst parameters)
        {
            var results = new Dictionary<ParameterAst, IReadOnlyList<string>>();

            if (_parameterNames.Count == 0)
            {
                return results;
            }

            var visitor = new ReferenceCollectingPSAstVisitor(_parameterNames);
            if (parameters.Parameters is not null)
            {
                foreach (ParameterAst parameter in parameters.Parameters)
                {
                    if (!_parameterNames.Contains(GetParameterName(parameter)))
                    {
                        continue;
                    }

                    parameter.DefaultValue?.Visit(visitor);
                    results[parameter] = visitor.References.Keys.ToList();
                    visitor.Reset();
                }
            }
            return results;
        }

        protected override string GetParameterName(ParameterAst parameter)
        {
            return parameter.Name.VariablePath.UserPath;
        }
    }
}

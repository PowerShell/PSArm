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
        <TArmParameter> : TemplateParameterConstructor<TArmParameter, ParamBlockAst, ParameterAst, string>
        where TArmParameter : ArmElement, IArmReferenceable
    {

        protected readonly PowerShell _pwsh;

        private readonly HashSet<string> _parameterNames;

        public PowerShellArmTemplateParameterConstructor(
            PowerShell pwsh,
            HashSet<string> parameterNames)
        {
            _pwsh = pwsh;
            _parameterNames = parameterNames;
        }


        protected ArmElement GetParameterValue(ParameterAst parameter)
        {
            if (parameter.DefaultValue is null)
            {
                return null;
            }

            _pwsh.Commands.Clear();

            Collection<PSObject> result = _pwsh.AddScript(parameter.DefaultValue.Extent.Text).Invoke();

            object input = result.Count == 1 ? result[0] : result;

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

                    parameter.DefaultValue.Visit(visitor);
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

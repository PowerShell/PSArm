using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm
{
    [Alias("Arm")]
    [Cmdlet(VerbsCommon.New, "ArmTemplate")]
    public class NewArmTemplateCommand : PSCmdlet
    {
        private static Version s_defaultVersion = new Version(1, 0, 0, 0);

        [Parameter()]
        public Version ContentVersion { get; set; } = s_defaultVersion;

        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            var armTemplate = new ArmTemplate()
            {
                ContentVersion = ContentVersion,
            };

            (ScriptBlock parameterizedBody, ArmParameter[] armParameters) = ParameterizeScriptBlock(Body);

            armTemplate.Parameters = armParameters;

            foreach (PSObject item in InvokeCommand.InvokeScript(SessionState, parameterizedBody, armParameters))
            {
                switch (item.BaseObject)
                {
                    case ArmResource resource:
                        armTemplate.Resources.Add(resource);
                        continue;

                    case ArmOutput output:
                        armTemplate.Outputs.Add(output);
                        continue;
                }
            }
            WriteObject(armTemplate);
        }

        private (ScriptBlock, ArmParameter[]) ParameterizeScriptBlock(ScriptBlock sb)
        {
            var ast = (ScriptBlockAst)sb.Ast;
            if (ast.ParamBlock?.Parameters == null || ast.ParamBlock.Parameters.Count == 0)
            {
                return (sb, Array.Empty<ArmParameter>());
            }

            var armParameters = new List<ArmParameter>();
            var parameterAsts = new List<ParameterAst>();
            foreach (ParameterAst parameter in ast.ParamBlock.Parameters)
            {
                var armParameter = new ArmParameter(parameter.Name.VariablePath.UserPath);

                // Go through attributes
                var attributes = new List<AttributeBaseAst>();
                if (parameter.Attributes != null && parameter.Attributes.Count > 0)
                {
                    foreach (AttributeBaseAst attributeBase in parameter.Attributes)
                    {
                        switch (attributeBase)
                        {
                            case TypeConstraintAst typeConstraint:
                                attributes.Add(
                                    new TypeConstraintAst(
                                        typeConstraint.Extent,
                                        new TypeName(
                                            typeConstraint.TypeName.Extent,
                                            "PSArm.ArmParameter")));
                                armParameter.Type = typeConstraint.TypeName.FullName;
                                continue;

                            case AttributeAst attribute:
                                if (string.Equals(attribute.TypeName.FullName, "ValidateSet", StringComparison.OrdinalIgnoreCase))
                                {
                                    var allowedValues = new List<object>(attribute.PositionalArguments.Count);
                                    foreach (ExpressionAst expr in attribute.PositionalArguments)
                                    {
                                        allowedValues.Add(expr.SafeGetValue());
                                    }
                                    armParameter.AllowedValues = allowedValues.ToArray();
                                }
                                continue;
                        }
                    }
                }

                if (parameter.DefaultValue != null)
                {
                    armParameter.DefaultValue = parameter.DefaultValue.SafeGetValue();
                }

                armParameters.Add(armParameter);
                parameterAsts.Add(
                    new ParameterAst(
                        parameter.Extent,
                        (VariableExpressionAst)parameter.Name.Copy(),
                        attributes,
                        defaultValue: null));
            }

            var newParamBlock = new ParamBlockAst(
                ast.ParamBlock.Extent,
                CopyAstCollection<AttributeAst>(ast.ParamBlock.Attributes),
                parameterAsts);

            var newScriptBlockAst = new ScriptBlockAst(
                ast.Extent,
                newParamBlock,
                (NamedBlockAst)ast.BeginBlock?.Copy(),
                (NamedBlockAst)ast.ProcessBlock?.Copy(),
                (NamedBlockAst)ast.EndBlock?.Copy(),
                (NamedBlockAst)ast.DynamicParamBlock?.Copy());

            return (newScriptBlockAst.GetScriptBlock(), armParameters.ToArray());
        }

        private List<TAst> CopyAstCollection<TAst>(IReadOnlyCollection<TAst> asts) where TAst : Ast
        {
            if (asts == null)
            {
                return null;
            }

            var acc = new List<TAst>(asts.Count);
            foreach (TAst ast in asts)
            {
                acc.Add((TAst)ast.Copy());
            }
            return acc;
        }
    }
}

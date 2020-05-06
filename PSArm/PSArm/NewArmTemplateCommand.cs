using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm
{
    [Alias("Arm")]
    [Cmdlet(VerbsCommon.New, "ArmTemplate")]
    public class NewArmTemplateCommand : PSCmdlet
    {
        private static ScriptPosition s_emptyPosition = new ScriptPosition(string.Empty, 0, 0, string.Empty);

        private static IScriptExtent s_emptyExtent = new ScriptExtent(s_emptyPosition, s_emptyPosition);

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

            (ScriptBlock parameterizedBody, ArmParameter[] armParameters, ArmVariable[] armVariables) = ParameterizeScriptBlock(Body);

            armTemplate.Parameters = armParameters;
            armTemplate.Variables = armVariables;

            var arguments = new List<object>();
            arguments.AddRange(armParameters);
            arguments.AddRange(armVariables);

            foreach (PSObject item in InvokeCommand.InvokeScript(SessionState, parameterizedBody, arguments.ToArray()))
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

        private (ScriptBlock, ArmParameter[], ArmVariable[]) ParameterizeScriptBlock(ScriptBlock sb)
        {

            var ast = (ScriptBlockAst)sb.Ast;
            ArmVariable[] armVariables = GatherVariables(ast, ast.ParamBlock?.Parameters ?? Enumerable.Empty<ParameterAst>());

            var armParameters = new List<ArmParameter>();
            var parameterAsts = new List<ParameterAst>();

            if (ast.ParamBlock?.Parameters != null)
            {
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
                        armParameter.DefaultValue = GetDefaultValue(parameter.DefaultValue);
                    }

                    armParameters.Add(armParameter);
                    parameterAsts.Add(
                        new ParameterAst(
                            parameter.Extent,
                            (VariableExpressionAst)parameter.Name.Copy(),
                            attributes,
                            defaultValue: null));
                }
            }

            foreach (ArmVariable variable in armVariables)
            {
                parameterAsts.Add(
                    new ParameterAst(
                        s_emptyExtent,
                        new VariableExpressionAst(s_emptyExtent, variable.Name, splatted: false),
                        Enumerable.Empty<AttributeBaseAst>(),
                        defaultValue: null));
            }

            ParamBlockAst newParamBlock;
            if (ast.ParamBlock != null)
            {
                newParamBlock = new ParamBlockAst(
                    s_emptyExtent,
                    Enumerable.Empty<AttributeAst>(),
                    parameterAsts);
            }
            else
            {
                newParamBlock = new ParamBlockAst(
                    ast.ParamBlock.Extent,
                    CopyAstCollection<AttributeAst>(ast.ParamBlock.Attributes),
                    parameterAsts);
             }

            var newScriptBlockAst = new ScriptBlockAst(
                ast.Extent,
                newParamBlock,
                (NamedBlockAst)ast.BeginBlock?.Copy(),
                (NamedBlockAst)ast.ProcessBlock?.Copy(),
                (NamedBlockAst)ast.EndBlock?.Copy(),
                (NamedBlockAst)ast.DynamicParamBlock?.Copy());

            return (newScriptBlockAst.GetScriptBlock(), armParameters.ToArray(), armVariables);
        }

        private object GetDefaultValue(ExpressionAst defaultValue)
        {
            foreach (PSObject result in InvokeCommand.InvokeScript(defaultValue.Extent.Text))
            {
                return ArmTypeConversion.Convert(result);
            }

            return null;
        }

        private ArmVariable[] GatherVariables(Ast ast, IEnumerable<ParameterAst> parameters)
        {
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "_",
                "psitem",
                "psscriptroot",
            };
            foreach (ParameterAst parameter in parameters)
            {
                exclude.Add(parameter.Name.VariablePath.UserPath);
            }

            var vars = new Dictionary<string, VariableExpressionAst>();
            foreach (VariableExpressionAst variable in ast.FindAll(subast => subast is VariableExpressionAst, searchNestedScriptBlocks: true))
            {
                if (!exclude.Contains(variable.VariablePath.UserPath))
                {
                    vars[variable.VariablePath.UserPath] = variable;
                }
            }

            var armVars = new List<ArmVariable>();
            foreach (string variableName in vars.Keys)
            {
                object value = SessionState.PSVariable.GetValue(variableName);
                armVars.Add(new ArmVariable(variableName, ArmTypeConversion.Convert(value)));
            }

            return armVars.ToArray();
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

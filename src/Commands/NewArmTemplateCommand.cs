using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using PSArm.ArmBuilding;
using PSArm.Completion;
using PSArm.Expression;
using PSArm.Internal;

namespace PSArm.Commands
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

            var armParameters = new List<ArmParameter>();
            var armVariables = new List<ArmVariable>();
            var parameterAsts = new List<ParameterAst>();

            if (ast.ParamBlock?.Parameters != null)
            {
                foreach (ParameterAst parameter in ast.ParamBlock.Parameters)
                {
                    ArmParameter armParameter = null;
                    object[] parameterAllowedValues = null;

                    // Go through attributes
                    bool isVariable = false;
                    var attributes = new List<AttributeBaseAst>();
                    if (parameter.Attributes != null && parameter.Attributes.Count > 0)
                    {
                        foreach (AttributeBaseAst attributeBase in parameter.Attributes)
                        {
                            switch (attributeBase)
                            {
                                case TypeConstraintAst typeConstraint:

                                    Type reflectedType = typeConstraint.TypeName.GetReflectionType();

                                    if (reflectedType == typeof(ArmVariable)
                                        || typeConstraint.TypeName.FullName.Is(ArmTypeAccelerators.ArmVariable))
                                    {
                                        isVariable = true;
                                        armVariables.Add(new ArmVariable(
                                            parameter.Name.VariablePath.UserPath,
                                            ArmTypeConversion.Convert(GetDefaultValue(parameter.DefaultValue))));
                                        parameterAsts.Add((ParameterAst)parameter.Copy());
                                        continue;
                                    }

                                    if (reflectedType != null && typeof(ArmParameter).IsAssignableFrom(reflectedType)
                                        || typeConstraint.TypeName.FullName.Is(ArmTypeAccelerators.ArmParameter)
                                        || typeConstraint.TypeName is GenericTypeName genericTypeName && genericTypeName.TypeName.FullName.Is(ArmTypeAccelerators.ArmParameter))
                                    {
                                    }

                                    Type parameterType = null;
                                    if (reflectedType != null && reflectedType.IsGenericType)
                                    {
                                        parameterType = reflectedType.GetGenericArguments()[0];
                                        attributes.Add(
                                            new TypeConstraintAst(
                                                typeConstraint.Extent,
                                                new TypeName(
                                                    typeConstraint.TypeName.Extent,
                                                    typeof(ArmParameter).FullName)));
                                    }

                                    armParameter = new ArmParameter(parameter.Name.VariablePath.UserPath)
                                    {
                                        Type = parameterType, 
                                        AllowedValues = parameterAllowedValues,
                                    };

                                    continue;

                                case AttributeAst attribute:
                                    if (string.Equals(attribute.TypeName.FullName, "ValidateSet", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var allowedValues = new List<object>(attribute.PositionalArguments.Count);
                                        foreach (ExpressionAst expr in attribute.PositionalArguments)
                                        {
                                            allowedValues.Add(expr.SafeGetValue());
                                        }
                                        parameterAllowedValues = allowedValues.ToArray();
                                    }
                                    continue;
                            }
                        }
                    }

                    if (isVariable)
                    {
                        continue;
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

            return (newScriptBlockAst.GetScriptBlock(), armParameters.ToArray(), armVariables.ToArray());
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

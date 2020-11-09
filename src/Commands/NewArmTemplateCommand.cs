
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Security;
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

            (ScriptBlock parameterizedBody, List<ArmParameter> armParameters, List<ArmVariable> armVariables) = ParameterizeScriptBlock(Body);

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

        private (ScriptBlock, List<ArmParameter>, List<ArmVariable>) ParameterizeScriptBlock(ScriptBlock sb)
        {

            var ast = (ScriptBlockAst)sb.Ast;

            var armParameters = new List<ArmParameter>();
            var armVariables = new List<ArmVariable>();
            var parameterAsts = new List<ParameterAst>();

            if (ast.ParamBlock?.Parameters != null)
            {
                foreach (ParameterAst parameter in ast.ParamBlock.Parameters)
                {
                    TypeConstraintAst typeConstraintAst = null;
                    if (parameter.Attributes != null && parameter.Attributes.Count > 0)
                    {
                        foreach (AttributeBaseAst attributeBaseAst in parameter.Attributes)
                        {
                            if (attributeBaseAst is TypeConstraintAst foundTypeConstraint)
                            {
                                typeConstraintAst = foundTypeConstraint;
                                break;
                            }
                        }
                    }

                    if (typeConstraintAst != null)
                    {
                        ParameterAst newParameterAst = null;

                        if (TryGetArmVariableFromPSParameter(typeConstraintAst, parameter, out ArmVariable armVariable, out newParameterAst))
                        {
                            armVariables.Add(armVariable);
                            parameterAsts.Add(newParameterAst);
                            continue;
                        }

                        if (TryGetArmParameterFromPSParameter(typeConstraintAst, parameter, out ArmParameter armParameter, out newParameterAst))
                        {
                            armParameters.Add(armParameter);
                            parameterAsts.Add(newParameterAst);
                            continue;
                        }
                    }

                    throw new ArgumentException($"Unable to convert parameter '{parameter.Name}' to ARM parameter or variable");
                }
            }

            ParamBlockAst newParamBlock = null;
            if (ast.ParamBlock != null)
            {
                newParamBlock = new ParamBlockAst(
                    ast.ParamBlock.Extent,
                    CopyAstCollection(ast.ParamBlock.Attributes),
                    parameterAsts);
            }

            var newScriptBlockAst = new ScriptBlockAst(
                ast.Extent,
                newParamBlock,
                (NamedBlockAst)ast.BeginBlock?.Copy(),
                (NamedBlockAst)ast.ProcessBlock?.Copy(),
                (NamedBlockAst)ast.EndBlock?.Copy(),
                (NamedBlockAst)ast.DynamicParamBlock?.Copy());

            return (newScriptBlockAst.GetScriptBlock(), armParameters, armVariables);
        }

        private bool TryGetArmParameterFromPSParameter(
            TypeConstraintAst typeConstraintAst,
            ParameterAst parameterAst,
            out ArmParameter armParameter,
            out ParameterAst newParameterAst)
        {
            Type reflectionType = typeConstraintAst.TypeName.GetReflectionType();

            // Non-generic ARM parameter: no type constraint
            armParameter = null;
            if (reflectionType == typeof(ArmParameter)
                || typeConstraintAst.TypeName.FullName.Is(ArmTypeAccelerators.ArmParameter))
            {
                armParameter = new ArmParameter(parameterAst.Name.VariablePath.UserPath);
            }
            else if (typeConstraintAst.TypeName is GenericTypeName genericTypeName
                    && (genericTypeName.TypeName.GetReflectionType() == typeof(ArmParameter)
                        || genericTypeName.TypeName.FullName.Is(ArmTypeAccelerators.ArmParameter)))
            {
                armParameter = new ArmParameter(parameterAst.Name.VariablePath.UserPath)
                {
                    Type = GetArmParameterTypeFromPSTypeName(typeConstraintAst.TypeName),
                };
            }

            if (armParameter == null)
            {
                newParameterAst = null;
                return false;
            }

            foreach (AttributeBaseAst parameterAttribute in parameterAst.Attributes)
            {
                if (!(parameterAttribute is AttributeAst attribute))
                {
                    continue;
                }

                Type attributeType = attribute.TypeName.GetReflectionAttributeType();

                if (attributeType == typeof(ValidateSetAttribute)
                    || attribute.TypeName.FullName.Is("ValidateSetAttribute"))
                {
                    var allowedValues = new List<IArmValue>();
                    foreach (ExpressionAst allowedExpression in attribute.PositionalArguments)
                    {
                        allowedValues.Add(ArmTypeConversion.Convert(allowedExpression.SafeGetValue()));
                    }
                    armParameter.AllowedValues = allowedValues;
                }
            }

            armParameter.DefaultValue = GetDefaultValue(parameterAst.DefaultValue);

            // Construct a new parameter AST that will accept the ARM parameter placeholder object we will give it
            newParameterAst = new ParameterAst(
                parameterAst.Extent,
                (VariableExpressionAst)parameterAst.Name.Copy(),
                Enumerable.Empty<AttributeAst>(), // TODO: Add a type constraint here
                defaultValue: null);

            return true;
        }

        private bool TryGetArmVariableFromPSParameter(
            TypeConstraintAst typeConstraintAst,
            ParameterAst parameterAst,
            out ArmVariable armVariable,
            out ParameterAst newParameterAst)
        {
            if (typeConstraintAst.TypeName.GetReflectionType() != typeof(ArmVariable)
                && !typeConstraintAst.TypeName.FullName.Is(ArmTypeAccelerators.ArmVariable))
            {
                armVariable = null;
                newParameterAst = null;
                return false;
            }

            armVariable = new ArmVariable(
                parameterAst.Name.VariablePath.UserPath,
                GetDefaultValue(parameterAst.DefaultValue));
            newParameterAst = (ParameterAst)parameterAst.Copy();
            return true;
        }

        private Type GetArmParameterTypeFromPSTypeName(ITypeName typeName)
        {
            if (!(typeName is GenericTypeName genericTypeName
                && genericTypeName.GenericArguments.Count == 1
                && genericTypeName.GenericArguments[0] is TypeName typeParameterName))
            {
                throw new ArgumentException($"Cannot convert typename '{typeName}' to ARM parameter type");
            }

            Type reflectionType = typeParameterName.GetReflectionType();
            if (reflectionType != null)
            {
                return reflectionType;
            }

            if (typeParameterName.FullName.Is("string"))
            {
                return typeof(string);
            }

            if (typeParameterName.FullName.Is("object"))
            {
                return typeof(object);
            }

            if (typeParameterName.FullName.Is("array"))
            {
                return typeof(Array);
            }

            if (typeParameterName.FullName.Is("securestring"))
            {
                return typeof(SecureString);
            }

            if (typeParameterName.FullName.Is("int"))
            {
                return typeof(int);
            }

            if (typeParameterName.FullName.Is("secureobject"))
            {
                return typeof(SecureObject);
            }

            if (typeParameterName.FullName.Is("bool"))
            {
                return typeof(bool);
            }

            throw new ArgumentException($"Cannot convert typename '{typeName}' to ARM parameter type");
        }

        private IArmValue GetDefaultValue(ExpressionAst defaultValue)
        {
            if (defaultValue == null)
            {
                return null;
            }

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

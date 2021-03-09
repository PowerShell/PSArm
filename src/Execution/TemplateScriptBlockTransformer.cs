
// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using PSArm.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm.Execution
{
    internal class TemplateScriptBlockTransformer
    {
        private static readonly Type[] s_armParameterCtorArgTypes = new[] { typeof(IArmString) };

        private readonly PowerShell _pwsh;

        public TemplateScriptBlockTransformer(PowerShell pwsh)
        {
            _pwsh = pwsh;
        }

        public ScriptBlock GetDeparameterizedTemplateScriptBlock(
            ScriptBlock scriptBlock,
            out ArmObject<ArmParameter> armParameters,
            out ArmObject<ArmVariable> armVariables)
        {
            var ast = (ScriptBlockAst)scriptBlock.Ast;

            if (ast.ParamBlock?.Parameters is null)
            {
                armParameters = null;
                armVariables = null;
                return scriptBlock;
            }

            var parameters = new ArmObject<ArmParameter>();
            var variables = new ArmObject<ArmVariable>();
            var parameterAsts = new List<ParameterAst>();

            // We would prefer not to clone ASTs, since doing so can lead to variable table issues that we can't handle
            bool mustCloneParameterAsts = false;

            foreach (ParameterAst parameter in ast.ParamBlock.Parameters)
            {
                if (!TryGetTypeConstraint(parameter, out TypeConstraintAst typeConstraint))
                {
                    throw new ArgumentException($"Unable to convert parameter '{parameter.Name}' to ARM parameter: no type constraint on parameter");
                }

                if (TryGetArmVariableFromPSParameter(typeConstraint, parameter, out ArmVariable armVariable))
                {
                    variables[armVariable.Name] = armVariable;
                    parameterAsts.Add(parameter);
                    continue;
                }

                if (TryGetArmParameterFromPSParameter(typeConstraint, parameter, out ArmParameter armParameter, out ParameterAst newParameterAst))
                {
                    parameters[armParameter.Name] = armParameter;
                    parameterAsts.Add(newParameterAst);

                    if (!ReferenceEquals(newParameterAst, parameter))
                    {
                        mustCloneParameterAsts = true;
                    }

                    continue;
                }

                throw new ArgumentException($"Unable to convert parameter '{parameter.Name}' to ARM parameter: bad type constraint '{typeConstraint.TypeName}'");
            }

            armParameters = parameters;
            armVariables = variables;

            if (!mustCloneParameterAsts)
            {
                return scriptBlock;
            }

            var newParamBlock = new ParamBlockAst(
                ast.ParamBlock.Extent,
                CopyAstCollection(ast.Attributes),
                CopyAstCollection(parameterAsts));

            return new ScriptBlockAst(
                ast.Extent,
                newParamBlock,
                (NamedBlockAst)ast.BeginBlock?.Copy(),
                (NamedBlockAst)ast.ProcessBlock?.Copy(),
                (NamedBlockAst)ast.EndBlock?.Copy(),
                (NamedBlockAst)ast.DynamicParamBlock?.Copy()).GetScriptBlock();
        }

        private bool TryGetTypeConstraint(ParameterAst parameter, out TypeConstraintAst typeConstraint)
        {
            if (parameter.Attributes is not null)
            {
                foreach (AttributeBaseAst attributeAst in parameter.Attributes)
                {
                    if (attributeAst is TypeConstraintAst foundTypeConstraint)
                    {
                        typeConstraint = foundTypeConstraint;
                        return true;
                    }
                }
            }

            typeConstraint = null;
            return false;
        }

        private bool TryGetArmVariableFromPSParameter(
            TypeConstraintAst typeConstraint,
            ParameterAst parameter,
            out ArmVariable armVariable)
        {
            if (typeConstraint.TypeName.GetReflectionType() != typeof(ArmVariable)
                && !typeConstraint.TypeName.FullName.Is(ArmTypeAccelerators.ArmVariable))
            {
                armVariable = null;
                return false;
            }

            armVariable = new ArmVariable(
                new ArmStringLiteral(parameter.Name.VariablePath.UserPath),
                GetDefaultValue(parameter.DefaultValue));
            return true;
        }

        private bool TryGetArmParameterFromPSParameter(
            TypeConstraintAst typeConstraint,
            ParameterAst parameter,
            out ArmParameter armParameter,
            out ParameterAst newParameterAst)
        {
            if (!TryGetArmParameterFromTypeConstraint(typeConstraint, parameter.Name.VariablePath.UserPath, out armParameter))
            {
                armParameter = null;
                newParameterAst = null;
                return false;
            }

            if (parameter.DefaultValue is not null)
            {
                armParameter.DefaultValue = GetDefaultValue(parameter.DefaultValue);
            }

            bool canReuseExistingAst = true;
            foreach (AttributeBaseAst attributeBase in parameter.Attributes)
            {
                if (attributeBase is not AttributeAst attribute
                    || attribute.TypeName.GetReflectionType() != typeof(ValidateSetAttribute)
                    || attribute.PositionalArguments is null
                    || attribute.PositionalArguments.Count == 0)
                {
                    continue;
                }

                var allowedValues = new ArmArray();
                foreach (ExpressionAst allowedExpression in attribute.PositionalArguments)
                {
                    if (!ArmElementConversion.TryConvertToArmElement(allowedExpression.SafeGetValue(), out ArmElement allowedArmElement))
                    {
                        armParameter = null;
                        newParameterAst = null;
                        return false;
                    }

                    allowedValues.Add(allowedArmElement);
                }

                canReuseExistingAst = false;
                armParameter.AllowedValues = allowedValues;
                break;
            }

            newParameterAst = canReuseExistingAst
                ? parameter
                : new ParameterAst(
                    parameter.Extent,
                    (VariableExpressionAst)parameter.Name.Copy(),
                    Enumerable.Empty<AttributeAst>(),
                    defaultValue: null);
            return true;
        }

        private bool TryGetArmParameterFromTypeConstraint(
            TypeConstraintAst typeConstraint,
            string parameterName,
            out ArmParameter armParameter)
        {
            Type paramType = typeConstraint.TypeName.GetReflectionType();
            
            if (paramType is not null)
            {
                if (!typeof(ArmParameter).IsAssignableFrom(paramType)
                    || !paramType.IsGenericType)
                {
                    armParameter = null;
                    return false;
                }

                // Construct the correct generic and allow the constructor to do the validation
                armParameter = (ArmParameter)paramType
                    .GetConstructor(s_armParameterCtorArgTypes)
                    .Invoke(new[] { new ArmStringLiteral(parameterName) });
                return true;
            }

            if (typeConstraint.TypeName is not GenericTypeName genericTypeName
                || !genericTypeName.TypeName.FullName.Is(ArmTypeAccelerators.ArmParameter))
            {
                armParameter = null;
                return false;
            }

            // We have an accelerated generic here, so must manually build the generic type to use
            ITypeName genericArg = genericTypeName.GenericArguments[0];
            Type genericType = genericArg.GetReflectionType();

            if (genericType is null)
            {
                armParameter = null;
                return false;
            }

            armParameter = (ArmParameter)typeof(ArmParameter<>)
                .MakeGenericType(genericType)
                .GetConstructor(s_armParameterCtorArgTypes)
                .Invoke(new[] { new ArmStringLiteral(parameterName) });
            return true;
        }

        private ArmElement GetDefaultValue(ExpressionAst expression)
        {
            if (expression is null)
            {
                return null;
            }

            _pwsh.Commands.Clear();
            foreach (PSObject result in _pwsh.AddScript(expression.Extent.Text).Invoke())
            {
                ArmElementConversion.TryConvertToArmElement(result, out ArmElement armElement);
                return armElement;
            }

            return null;
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

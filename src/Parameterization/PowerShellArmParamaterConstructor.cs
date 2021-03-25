
// Copyright (c) Microsoft Corporation.

using PSArm.Templates;
using PSArm.Templates.Primitives;
using PSArm.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSArm.Parameterization
{
    internal class PowerShellArmParameterConstructor
        : PowerShellArmTemplateParameterConstructor<ArmParameter>
    {
        private static readonly ConcurrentDictionary<Type, Func<IArmString, ArmParameter>> s_armParameterConstructors = new ConcurrentDictionary<Type, Func<IArmString, ArmParameter>>();

        private readonly HashSet<string> _parametersWithAllowedValues;

        public PowerShellArmParameterConstructor(PowerShell pwsh, HashSet<string> parameterNames, HashSet<string> parametersWithAllowedValues)
            : base(pwsh, parameterNames)
        {
            _parametersWithAllowedValues = parametersWithAllowedValues;
        }

        protected override ArmParameter EvaluateParameter(ParameterAst parameter)
        {
            Type parameterType = GetParameterGenericType(parameter);

            if (!ArmTypeConversion.TryConvertToArmType(parameterType, out _))
            {
                throw new ArgumentException($"Parameter '{parameter}' has invalid ARM type '{parameterType}'");
            }

            Func<IArmString, ArmParameter> factory = s_armParameterConstructors.GetOrAdd(parameterType, CreateArmParameterFactory);

            ArmParameter armParameter = factory(new ArmStringLiteral(GetParameterName(parameter)));

            if (parameter.DefaultValue is not null)
            {
                armParameter.DefaultValue = GetParameterValue(parameter);
            }

            if (TryGetAllowedValues(parameter, out ArmArray allowedValues))
            {
                _parametersWithAllowedValues.Add(GetParameterName(parameter));
                armParameter.AllowedValues = allowedValues;
            }

            return armParameter;
        }

        private bool TryGetAllowedValues(ParameterAst parameter, out ArmArray allowedValues)
        {
            if (parameter.Attributes is null)
            {
                allowedValues = null;
                return false;
            }

            foreach (AttributeBaseAst attributeBase in parameter.Attributes)
            {
                if (attributeBase is not AttributeAst attribute
                    || attribute.TypeName.GetReflectionType() != typeof(ValidateSetAttribute)
                    || attribute.PositionalArguments is null
                    || attribute.PositionalArguments.Count == 0)
                {
                    continue;
                }

                allowedValues = new ArmArray();
                foreach (ExpressionAst allowedExpression in attribute.PositionalArguments)
                {
                    if (!ArmElementConversion.TryConvertToArmElement(allowedExpression.SafeGetValue(), out ArmElement allowedArmElement))
                    {
                        throw new ArgumentException($"ValidateSet value '{allowedExpression}' on parameter '{parameter.Name}' is not a value ARM value");
                    }

                    allowedValues.Add(allowedArmElement);
                }

                return true;
            }

            allowedValues = null;
            return false;
        }

        private Type GetParameterGenericType(ParameterAst parameter)
        {
            if (parameter.Attributes is null)
            {
                throw new ArgumentException($"Parameter '{parameter}' must have a type constraint");
            }

            TypeConstraintAst typeConstraint = null;
            foreach (AttributeBaseAst attribute in parameter.Attributes)
            {
                if (attribute is TypeConstraintAst foundTypeConstraint)
                {
                    typeConstraint = foundTypeConstraint;
                    break;
                }
            }

            if (typeConstraint is null)
            {
                throw new ArgumentException($"Parameter '{parameter}' must have a type constraint");
            }

            if (typeConstraint.TypeName is not GenericTypeName genericTypeName)
            {
                throw new ArgumentException($"Parameter '{parameter}' must declare a type of the form '[ArmParameter[TYPE]]'");
            }

            return genericTypeName.GenericArguments[0].GetReflectionType();
        }

        private static Func<IArmString, ArmParameter> CreateArmParameterFactory(Type parameterType)
        {
            // To amortize the expense of all this reflection and type creation,
            // we instead compile a factory lambda around the constructor.
            // This way we only have to work hard once per ARM type, rather than for every parameter.

            ConstructorInfo ctor = typeof(ArmParameter<>)
                .MakeGenericType(new Type[] { parameterType })
                .GetConstructor(new Type[] { typeof(IArmString) });

            var delegateParameters = new ParameterExpression[] { Expression.Parameter(typeof(IArmString)) };
            return Expression
                .Lambda<Func<IArmString, ArmParameter>>(
                    Expression.New(ctor, delegateParameters),
                    delegateParameters)
                .Compile();
        }
    }
}

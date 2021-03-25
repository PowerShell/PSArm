
// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Templates;
using PSArm.Types;
using System.Management.Automation.Language;

namespace PSArm.Parameterization
{
    internal static class PSArmParameterization
    {
        public static PSArmVarType GetPSArmVarType(ParameterAst parameterAst)
        {
            if (!TryGetTypeConstraint(parameterAst, out TypeConstraintAst typeConstraintAst))
            {
                return PSArmVarType.None;
            }

            if (IsVariableType(typeConstraintAst.TypeName))
            {
                return PSArmVarType.Variable;
            }

            if (IsParameterType(typeConstraintAst.TypeName))
            {
                return PSArmVarType.Parameter;
            }

            return PSArmVarType.None;
        }

        private static bool TryGetTypeConstraint(ParameterAst parameter, out TypeConstraintAst typeConstraint)
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

        private static bool IsVariableType(ITypeName typeName)
        {
            return typeName is TypeName simpleTypeName
                && (simpleTypeName.FullName.Is(ArmTypeAccelerators.ArmVariable) || simpleTypeName.GetReflectionType() == typeof(ArmVariable));
        }

        private static bool IsParameterType(ITypeName typeName)
        {
            if (typeName is not GenericTypeName genericType)
            {
                return false;
            }

            if (genericType.FullName.Is(ArmTypeAccelerators.ArmParameter))
            {
                return true;
            }

            return genericType.GetReflectionType()?.GetGenericTypeDefinition() == typeof(ArmParameter<>);
        }
    }

    internal enum PSArmVarType
    {
        None = 0,
        Variable,
        Parameter,
    }
}

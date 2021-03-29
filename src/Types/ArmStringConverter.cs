
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System;
using System.Management.Automation;

namespace PSArm.Types
{
    public class ArmStringConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (sourceValue is null)
            {
                return false;
            }

            if (!typeof(IArmString).IsAssignableFrom(destinationType))
            {
                return false;
            }

            Type sourceType = sourceValue.GetType();

            return sourceType == typeof(string)
                || sourceType == typeof(ArmStringLiteral)
                || sourceType == typeof(ArmParameter)
                || sourceType == typeof(ArmVariable)
                || typeof(ArmOperation).IsAssignableFrom(sourceType);
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return false;
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            switch (sourceValue)
            {
                case string str:
                    return new ArmStringLiteral(str);

                case ArmStringLiteral armStr:
                    return armStr;

                case ArmOperation armExpr:
                    return armExpr;

                case ArmParameter armParameter:
                    return (ArmParameterReferenceExpression)armParameter;

                case ArmVariable armVariable:
                    return (ArmVariableReferenceExpression)armVariable;

                default:
                    throw new InvalidCastException($"Unable to cast value '{sourceValue}' of type '{sourceValue.GetType()}' to type '{destinationType}'");
            }
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            throw new NotImplementedException();
        }
    }
}

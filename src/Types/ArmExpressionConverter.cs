
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using System;
using System.Management.Automation;

namespace PSArm.Types
{
    public class ArmExpressionConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (sourceValue is null)
            {
                return true;
            }

            if (Type.GetTypeCode(sourceValue.GetType()) != TypeCode.Object)
            {
                return true;
            }

            return sourceValue is ArmExpression;
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return false;
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (!ArmElementConversion.TryConvertToArmExpression(sourceValue, out ArmExpression armExpression))
            {
                throw new InvalidCastException($"Value of type '{sourceValue.GetType()}' could not be converted to type '{destinationType}'");
            }

            return armExpression;
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            throw new NotImplementedException();
        }
    }
}

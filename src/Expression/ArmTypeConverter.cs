using System;
using System.Management.Automation;

namespace PSArm.Expression
{
    public class ArmTypeConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            switch (sourceValue)
            {
                case string _:
                    return destinationType.IsAssignableFrom(typeof(ArmStringLiteral));

                case int _:
                    return destinationType.IsAssignableFrom(typeof(ArmIntLiteral));

                case bool _:
                    return destinationType.IsAssignableFrom(typeof(ArmBoolLiteral));

                default:
                    return false;
            }
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return CanConvertFrom(sourceValue, destinationType);
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return ArmTypeConversion.Convert(sourceValue);
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return ConvertFrom(sourceValue, destinationType, formatProvider, ignoreCase);
        }
    }

}
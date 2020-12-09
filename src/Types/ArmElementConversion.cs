using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System;
using System.Collections;

namespace PSArm.Types
{
    public static class ArmElementConversion
    {
        public static bool TryConvertToArmString(object value, out IArmString armString)
        {
            switch (value)
            {
                case string str:
                    armString = new ArmStringValue(str);
                    return true;

                case ArmOperation expr:
                    armString = expr;
                    return true;

                case ArmStringValue strVal:
                    armString = strVal;
                    return true;

                case IArmString armStr:
                    armString = armStr;
                    return true;
            }

            armString = null;
            return false;
        }

        public static bool TryConvertToArmElement(object value, out ArmElement armElement)
        {
            if (value is null)
            {
                armElement = ArmNullValue.Value;
                return true;
            }

            Type type = value.GetType();
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                case TypeCode.DateTime:
                case TypeCode.Char:
                    armElement = new ArmStringValue(value.ToString());
                    return true;

                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Int16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.UInt16:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    armElement = new ArmIntegerValue((long)value);
                    return true;

                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    armElement = new ArmDoubleValue((double)value);
                    return true;

                case TypeCode.DBNull:
                    armElement = ArmNullValue.Value;
                    return true;
            }

            switch (value)
            {
                case ArmElement element:
                    armElement = element;
                    return true;

                case IDictionary dictionary:
                    return TryConvertDictionaryToArmObject(dictionary, out armElement);

                case IEnumerable enumerable:
                    return TryConvertEnumerableToArmArray(enumerable, out armElement);
            }

            armElement = null;
            return false;
        }

        private static bool TryConvertDictionaryToArmObject(IDictionary dictionary, out ArmElement armObject)
        {
            var obj = new ArmObject();
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!TryConvertToArmString(entry.Key, out IArmString key)
                    || !TryConvertToArmElement(entry.Value, out ArmElement value))
                {
                    armObject = null;
                    return false;
                }

                obj[key] = value;
            }

            armObject = obj;
            return true;
        }

        private static bool TryConvertEnumerableToArmArray(IEnumerable enumerable, out ArmElement armArray)
        {
            var array = new ArmArray();
            foreach (object element in enumerable)
            {
                if (!TryConvertToArmElement(element, out ArmElement armElement))
                {
                    armArray = null;
                    return false;
                }

                array.Add(armElement);
            }

            armArray = array;
            return true;
        }
    }
}

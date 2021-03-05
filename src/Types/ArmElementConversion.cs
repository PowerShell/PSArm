
// Copyright (c) Microsoft Corporation.

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
                    armString = new ArmStringLiteral(str);
                    return true;

                case IArmString armStr:
                    armString = armStr;
                    return true;
            }

            armString = null;
            return false;
        }

        public static bool TryConvertToArmExpression(object value, out ArmExpression armExpression)
        {
            if (value is null)
            {
                armExpression = ArmNullLiteral.Value;
                return true;
            }

            Type type = value.GetType();
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                case TypeCode.DateTime:
                case TypeCode.Char:
                    armExpression = new ArmStringLiteral(value.ToString());
                    return true;

                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Int16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.UInt16:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    armExpression = new ArmIntegerLiteral((long)value);
                    return true;

                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    armExpression = new ArmDoubleLiteral((double)value);
                    return true;

                case TypeCode.DBNull:
                    armExpression = ArmNullLiteral.Value;
                    return true;
            }

            if (value is ArmExpression inputArmExpression)
            {
                armExpression = inputArmExpression;
                return true;
            }

            armExpression = null;
            return false;
        }

        public static bool TryConvertToArmElement(object value, out ArmElement armElement)
        {
            if (TryConvertToArmExpression(value, out ArmExpression armExpression))
            {
                armElement = armExpression;
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

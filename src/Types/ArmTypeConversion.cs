
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Security;

namespace PSArm.Types
{
    public static class ArmTypeConversion
    {
        public static bool TryConvertToArmType(Type type, out ArmType? armType)
        {
            if (type is null)
            {
                armType = null;
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                case TypeCode.String:
                    armType = ArmType.String;
                    return true;

                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    armType = ArmType.Int;
                    return true;

                case TypeCode.Boolean:
                    armType = ArmType.Bool;
                    return true;
            }

            if (type == typeof(object))
            {
                armType = ArmType.Object;
                return true;
            }

            if (type == typeof(SecureString))
            {
                armType = ArmType.SecureString;
                return true;
            }

            if (type == typeof(SecureObject))
            {
                armType = ArmType.SecureObject;
                return true;
            }

            if (type == typeof(Array))
            {
                armType = ArmType.Array;
                return true;
            }

            armType = null;
            return false;
        }

        public static bool TryConvertToArmType(string type, out ArmType? armType)
        {
            if (Enum.TryParse(type, out ArmType parsedType))
            {
                armType = parsedType;
                return true;
            }

            armType = null;
            return false;
        }
    }
}

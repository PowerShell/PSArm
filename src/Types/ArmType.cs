using PSArm.Templates.Primitives;
using System;
using System.Security;

namespace PSArm.Types
{
    public enum ArmType
    {
        Null,
        String,
        SecureString,
        Int,
        Bool,
        Object,
        SecureObject,
        Array,
        Double,
    }

    public static class ArmTypeExtensions
    {
        private const string ArmType_String = "string";

        private const string ArmType_SecureString = "securestring";

        private const string ArmType_Int = "int";

        private const string ArmType_Bool = "bool";

        private const string ArmType_Object = "object";

        private const string ArmType_SecureObject = "secureObject";

        private const string ArmType_Array = "array";

        private static readonly ArmStringValue s_string = new ArmStringValue(ArmType_String);

        private static readonly ArmStringValue s_secureString = new ArmStringValue(ArmType_SecureString);

        private static readonly ArmStringValue s_int = new ArmStringValue(ArmType_Int);

        private static readonly ArmStringValue s_bool = new ArmStringValue(ArmType_Bool);

        private static readonly ArmStringValue s_object = new ArmStringValue(ArmType_Object);

        private static readonly ArmStringValue s_secureObject = new ArmStringValue(ArmType_SecureObject);

        private static readonly ArmStringValue s_array = new ArmStringValue(ArmType_Array);

        public static string AsString(this ArmType type)
        {
            switch (type)
            {
                case ArmType.String:
                    return ArmType_String;

                case ArmType.SecureString:
                    return ArmType_SecureString;

                case ArmType.Int:
                    return ArmType_Int;

                case ArmType.Bool:
                    return ArmType_Bool;

                case ArmType.Object:
                    return ArmType_Object;

                case ArmType.SecureObject:
                    return ArmType_SecureObject;

                case ArmType.Array:
                    return ArmType_Array;

                default:
                    throw new InvalidOperationException($"Cannot convert unsupported ARM type: '{type}'");
            }
        }

        public static ArmStringValue AsArmString(this ArmType type)
        {
            switch (type)
            {
                case ArmType.String:
                    return s_string;

                case ArmType.SecureString:
                    return s_secureString;

                case ArmType.Int:
                    return s_int;

                case ArmType.Bool:
                    return s_bool;

                case ArmType.Object:
                    return s_object;

                case ArmType.SecureObject:
                    return s_secureObject;

                case ArmType.Array:
                    return s_array;

                default:
                    throw new InvalidOperationException($"Cannot convert unsupported ARM type: '{type}'");
            }
        }

        public static Type AsType(this ArmType armType)
        {
            switch (armType)
            {
                case ArmType.String:
                    return typeof(string);

                case ArmType.SecureString:
                    return typeof(SecureString);

                case ArmType.Int:
                    return typeof(long);

                case ArmType.Bool:
                    return typeof(bool);

                case ArmType.Object:
                    return typeof(object);

                case ArmType.SecureObject:
                    return typeof(SecureObject);

                case ArmType.Array:
                    return typeof(Array);

                default:
                    throw new InvalidOperationException($"Cannot convert unsupported ARM type: '{armType}'");
            }
        }

    }
}


// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Types
{
    internal static class ArmBuiltinTypeExtensions
    {
        public static string AsPowerShellTypeString(this BuiltInTypeKind builtinType)
        {
            switch (builtinType)
            {
                case BuiltInTypeKind.Object:
                case BuiltInTypeKind.Null:
                case BuiltInTypeKind.Any:
                case BuiltInTypeKind.ResourceRef:
                    return "object";

                case BuiltInTypeKind.Array:
                    return "array";

                case BuiltInTypeKind.Bool:
                    return "bool";

                case BuiltInTypeKind.Int:
                    return "int";

                case BuiltInTypeKind.String:
                    return "string";

                default:
                    throw new ArgumentException($"Unknown ARM builtin type: '{builtinType}'");
            }
        }

        public static ArmType? AsArmType(this BuiltInTypeKind builtinType)
        {
            switch (builtinType)
            {
                case BuiltInTypeKind.Any:
                    return null;

                case BuiltInTypeKind.Array:
                    return ArmType.Array;

                case BuiltInTypeKind.Bool:
                    return ArmType.Bool;

                case BuiltInTypeKind.Int:
                    return ArmType.Int;

                case BuiltInTypeKind.Null:
                    return ArmType.Null;

                case BuiltInTypeKind.Object:
                    return ArmType.Object;

                case BuiltInTypeKind.ResourceRef:
                    return null;

                case BuiltInTypeKind.String:
                    return ArmType.String;

                default:
                    throw new ArgumentException($"Unknown ARM builtin type: '{builtinType}'");
            }
        }
    }
}

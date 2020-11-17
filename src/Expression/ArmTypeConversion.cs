
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections;
using System.Management.Automation;
using System.Security;

namespace PSArm.Expression
{
    /// <summary>
    /// Conversion logic between .NET types and ARM expression types.
    /// </summary>
    internal static class ArmTypeConversion
    {
        /// <summary>
        /// Convert a .NET object to an ARM expression.
        /// </summary>
        /// <param name="obj">The .NET object or value to convert.</param>
        /// <returns>An ARM expression representing the given .NET value.</returns>
        public static IArmValue Convert(object obj)
        {
            switch (obj)
            {
                case null:
                    return null;

                case IArmValue value:
                    return value;

                case PSObject psObj:
                    return Convert(psObj.BaseObject);

                case string s:
                    return new ArmStringLiteral(s);

                case int i:
                    return new ArmIntLiteral(i);

                case bool b:
                    return new ArmBoolLiteral(b);

                case IDictionary dict:
                    return ConvertDictionary(dict);

                case IEnumerable enumerable:
                    return ConvertEnumerable(enumerable);

                default:
                    throw new ArgumentException($"Unable to covert value '{obj}' of type '{obj.GetType()}' to IArmExpression");
            }
        }

        public static string GetArmTypeNameFromType(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(object))
            {
                return "object";
            }

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(SecureString))
            {
                return "securestring";
            }

            if (type == typeof(Array))
            {
                return "array";
            }

            if (type == typeof(SecureObject))
            {
                return "secureObject";
            }

            throw new ArgumentException($"Cannot convert type '{type}' to known ARM type");
        }

        private static ArmObject ConvertDictionary(IDictionary dict)
        {
            var armObj = new ArmObject();
            foreach (DictionaryEntry entry in dict)
            {
                armObj[entry.Key.ToString()] = Convert(entry.Value);
            }
            return armObj;
        }

        private static ArmArray ConvertEnumerable(IEnumerable enumerable)
        {
            var armArr = new ArmArray();
            foreach (object item in enumerable)
            {
                armArr.Add(Convert(item));
            }
            return armArr;
        }
    }

}
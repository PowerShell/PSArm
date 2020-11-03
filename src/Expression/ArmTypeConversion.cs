
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Management.Automation;

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

                default:
                    throw new ArgumentException($"Unable to covert value '{obj}' of type '{obj.GetType()}' to IArmExpression");
            }
        }
    }

}
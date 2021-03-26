
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// All rights reserved.

using System;

namespace PSArm.Internal
{
    /// <summary>
    /// Helper class to reduce boilerplate needed for string manipulation.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Check case-insensitive string equality.
        /// </summary>
        /// <param name="thisStr"></param>
        /// <param name="thatStr"></param>
        /// <returns>True if the strings are case-insensitively equal, false otherwise.</returns>
        public static bool Is(this string thisStr, string thatStr)
        {
            return string.Equals(thisStr, thatStr, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasPrefix(this string s, string prefix)
            => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Convert a string from PascalCase to camelCase.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>The camel-cased string.</returns>
        public static string CamelCase(this string s)
        {
            return char.IsLower(s[0])
                ? s
                : char.ToLower(s[0]) + s.Substring(1);
        }

        public static string PascalCase(this string s)
        {
            return char.IsUpper(s[0])
                ? s
                : char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string Depluralize(this string s)
        {
            int lastIdx = s.Length - 1;

            if (s[lastIdx] != 's')
            {
                return s;
            }

            return s.Substring(0, lastIdx);
        }
    }
}
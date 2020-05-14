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

        /// <summary>
        /// Convert a string from PascalCase to camelCase.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>The camel-cased string.</returns>
        public static string UnPascal(this string s)
        {
            return char.IsLower(s[0])
                ? s
                : char.ToLower(s[0]) + s.Substring(1);
        }
    }
}
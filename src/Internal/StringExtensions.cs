using System;

namespace PSArm.Internal
{
    internal static class StringExtensions
    {
        public static bool Is(this string thisStr, string thatStr)
        {
            return string.Equals(thisStr, thatStr, StringComparison.OrdinalIgnoreCase);
        }

        public static string UnPascal(this string s)
        {
            return char.IsLower(s[0])
                ? s
                : char.ToLower(s[0]) + s.Substring(1);
        }
    }
}
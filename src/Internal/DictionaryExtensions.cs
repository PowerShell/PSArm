using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Internal
{
    internal static class DictionaryExtensions
    {
        public static bool TryGetValue<TKey, TValue>(this IDictionary dictionary, TKey key, out TValue value)
        {
            if (dictionary.Contains(key))
            {
                value = (TValue)dictionary[key];
                return true;
            }

            value = default;
            return false;
        }
    }
}

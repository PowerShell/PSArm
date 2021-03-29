
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PSArm.Internal
{
    internal static class ListDictionaryExtensions
    {
        public static void AddToDictionaryList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        {
            if (!dictionary.TryGetValue(key, out List<TValue> list))
            {
                list = new List<TValue>();
                dictionary[key] = list;
            }

            list.Add(value);
        }
    }
}

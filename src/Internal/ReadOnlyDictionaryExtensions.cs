
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PSArm.Internal
{
    internal static class ReadOnlyDictionaryExtensions
    {
        public static IReadOnlyDictionary<TKey, TValue> ShallowClone<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> value)
        {
            var dict = new Dictionary<TKey, TValue>(value.Count);
            foreach (KeyValuePair<TKey, TValue> entry in value)
            {
                dict[entry.Key] = entry.Value;
            }
            return dict;
        }
    }
}

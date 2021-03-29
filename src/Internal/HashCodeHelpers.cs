// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PSArm.Internal
{
    internal static class HashCodeHelpers
    {
        public static int CombineHashCodes<T1, T2, T3>(T1 x1, T2 x2, T3 x3)
        {
            // From https://stackoverflow.com/a/34006336
            const int seed = 1009;
            const int factor = 9176;

            int hash = seed;
            hash = hash * factor + x1.GetHashCode();
            hash = hash * factor + x2.GetHashCode();
            hash = hash * factor + x3.GetHashCode();
            return hash;
        }
    }
}

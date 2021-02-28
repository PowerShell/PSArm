
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;

namespace PSArm.Internal
{
    internal static class ArmStringExtensions
    {
        public static ArmStringLiteral CoerceToLiteral(this IArmString armString)
        {
            return (ArmStringLiteral)armString;
        }

        public static string CoerceToString(this IArmString armString)
        {
            return armString.CoerceToLiteral().Value;
        }
    }
}

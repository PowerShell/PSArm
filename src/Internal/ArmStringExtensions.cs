using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

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

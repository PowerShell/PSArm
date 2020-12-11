using PSArm.Serialization;
using System.Runtime.CompilerServices;

namespace PSArm.Internal
{
    internal static class ArmExpressionTokenExtensions
    {
        public static string CoerceToString(this ArmExpressionToken token)
        {
            return ((ArmExpressionStringToken)token).Value;
        }

        public static long CoerceToLong(this ArmExpressionToken token)
        {
            return ((ArmExpressionIntegerToken)token).Value;
        }

        public static bool CoerceToBool(this ArmExpressionToken token)
        {
            return ((ArmExpressionBooleanToken)token).Value;
        }
    }
}

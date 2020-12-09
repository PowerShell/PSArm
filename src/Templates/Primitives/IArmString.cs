using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmStringConverter))]
    public interface IArmString : IArmExpression
    {
        string ToExpressionString();

        string ToIdentifierString();
    }
}

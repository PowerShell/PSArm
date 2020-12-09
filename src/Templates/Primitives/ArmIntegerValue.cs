using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public sealed class ArmIntegerValue : ArmValue<long>
    {
        public ArmIntegerValue(long value) : base(value, ArmType.Int)
        {
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToInnerExpressionString()
        {
            return Value.ToString();
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitIntegerValue(this);
    }
}

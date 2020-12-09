using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public sealed class ArmNullValue : ArmValue<object>
    {
        public static new ArmNullValue Value { get; } = new ArmNullValue();

        private ArmNullValue() : base(null, ArmType.Object)
        {
        }

        public override string ToInnerExpressionString()
        {
            return "json('null')";
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitNullValue(this);
    }
}

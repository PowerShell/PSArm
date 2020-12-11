using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public sealed class ArmNullLiteral : ArmLiteral<object>
    {
        public static new ArmNullLiteral Value { get; } = new ArmNullLiteral();

        private ArmNullLiteral() : base(null, ArmType.Object)
        {
        }

        public override string ToInnerExpressionString()
        {
            return "json('null')";
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitNullValue(this);
    }
}

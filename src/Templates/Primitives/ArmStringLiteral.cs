using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public sealed class ArmStringLiteral : ArmLiteral<string>, IArmString
    {
        public ArmStringLiteral(string value) : base(value, ArmType.String)
        {
        }

        public string ToExpressionString() => Value;

        public string ToIdentifierString() => Value;

        public override string ToInnerExpressionString() => $"'{Value}'";

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitStringValue(this);
    }
}

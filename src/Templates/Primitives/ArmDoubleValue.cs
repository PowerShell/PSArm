using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public class ArmDoubleValue : ArmValue<double>
    {
        public ArmDoubleValue(double value) : base(value, ArmType.Double)
        {
        }

        public override string ToInnerExpressionString()
        {
            return Value.ToString();
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitDoubleValue(this);
    }
}

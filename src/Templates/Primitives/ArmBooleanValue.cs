using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public class ArmBooleanValue : ArmValue<bool>
    {
        public static ArmBooleanValue True { get; } = new ArmBooleanValue(true);

        public static ArmBooleanValue False { get; } = new ArmBooleanValue(false);

        public static ArmBooleanValue FromBool(bool value) => value ? True : False;

        private ArmBooleanValue(bool value) : base(value, ArmType.Bool)
        {
        }

        public override string ToInnerExpressionString()
        {
            return Value ? "true" : "false";
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitBooleanValue(this);
    }
}

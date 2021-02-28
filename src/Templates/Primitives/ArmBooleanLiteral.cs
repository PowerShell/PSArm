
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public class ArmBooleanLiteral : ArmLiteral<bool>
    {
        public static ArmBooleanLiteral True { get; } = new ArmBooleanLiteral(true);

        public static ArmBooleanLiteral False { get; } = new ArmBooleanLiteral(false);

        public static ArmBooleanLiteral FromBool(bool value) => value ? True : False;

        private ArmBooleanLiteral(bool value) : base(value, ArmType.Bool)
        {
        }

        public override string ToInnerExpressionString()
        {
            return Value ? "true" : "false";
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitBooleanValue(this);
    }
}

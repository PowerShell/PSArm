
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Visitors;
using PSArm.Types;
using System.Collections.Generic;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public class ArmDoubleLiteral : ArmLiteral<double>
    {
        public ArmDoubleLiteral(double value) : base(value, ArmType.Double)
        {
        }

        public override string ToInnerExpressionString()
        {
            return Value.ToString();
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitDoubleValue(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => this;
    }
}

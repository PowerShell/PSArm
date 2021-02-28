
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Operations;
using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public sealed class ArmNullLiteral : ArmFunctionCallExpression
    {
        public static ArmNullLiteral Value { get; } = new ArmNullLiteral();

        private ArmNullLiteral() : base(new ArmStringLiteral("json"), new [] { new ArmStringLiteral("null") })
        {
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitNullValue(this);
    }
}

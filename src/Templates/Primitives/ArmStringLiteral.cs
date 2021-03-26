
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Internal;
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

        public string ToExpressionString()
        {
            string value = Value;
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                value = $"[{value}";
            }
            return value.Replace("\"", "\\\"");
        }

        public string ToIdentifierString() => Value.CamelCase();

        public override string ToInnerExpressionString() => $"'{Value}'";

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitStringValue(this);
    }
}

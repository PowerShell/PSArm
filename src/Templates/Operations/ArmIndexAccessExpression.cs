
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using System.Dynamic;

namespace PSArm.Templates.Operations
{
    using PSArm.Templates.Visitors;
    using System.Linq.Expressions;

    public class ArmIndexAccessExpression : ArmOperation
    {
        internal static DynamicMetaObject CreateMetaObject(
            DynamicMetaObject originalExpressionMO,
            GetIndexBinder binder,
            DynamicMetaObject[] indexes)
        {
            var indexAccess = new ArmIndexAccessExpression((ArmOperation)originalExpressionMO.Value, new ArmIntegerLiteral((long)indexes[0].Value));
            var expression = Expression.Constant(indexAccess);
            var restrictions = originalExpressionMO.Restrictions.Merge(binder.FallbackGetIndex(originalExpressionMO, indexes).Restrictions);
            return new DynamicMetaObject(expression, restrictions);
        }

        public ArmIndexAccessExpression()
        {
        }

        public ArmIndexAccessExpression(ArmOperation expression, ArmExpression index)
            : this()
        {
            InnerExpression = expression;
            Index = index;
        }

        public ArmOperation InnerExpression { get; set; }

        public ArmExpression Index { get; set; }

        public override string ToInnerExpressionString()
        {
            return $"{InnerExpression.ToInnerExpressionString()}[{Index.ToInnerExpressionString()}]";
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitIndexAccess(this);
    }
}

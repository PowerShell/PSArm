
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using System.Dynamic;

namespace PSArm.Templates.Operations
{
    using PSArm.Templates.Visitors;
    using System.Linq.Expressions;

    public class ArmMemberAccessExpression : ArmOperation
    {
        internal static DynamicMetaObject CreateMetaObject(
            DynamicMetaObject originalExpressionMO,
            GetMemberBinder binder)
        {
            var memberAccess = new ArmMemberAccessExpression((ArmOperation)originalExpressionMO.Value, new ArmStringLiteral(binder.Name));
            var expression = Expression.Constant(memberAccess);
            BindingRestrictions restrictions = originalExpressionMO.Restrictions.Merge(binder.FallbackGetMember(originalExpressionMO).Restrictions);
            return new DynamicMetaObject(expression, restrictions);
        }

        public ArmMemberAccessExpression()
        {
        }

        public ArmMemberAccessExpression(ArmOperation expression, IArmString member)
        {
            InnerExpression = expression;
            Member = member;
        }

        public ArmOperation InnerExpression { get; set; }

        public IArmString Member { get; set; }

        public override string ToInnerExpressionString()
        {
            return $"{InnerExpression.ToInnerExpressionString()}.{Member.ToIdentifierString()}";
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitMemberAccess(this);
    }
}

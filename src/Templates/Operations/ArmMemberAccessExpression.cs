
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;


namespace PSArm.Templates.Operations
{
    public class ArmMemberAccessExpression : ArmOperation
    {
        internal static DynamicMetaObject CreateMetaObject(
            DynamicMetaObject originalExpressionMO,
            GetMemberBinder binder)
        {
            // Generate an expression like this:
            //
            //     new ArmMemberAccessExpression((ArmOperation)dynamicObject, new ArmStringLiteral(binderName))
            //
            var expression = Expression.New(
                typeof(ArmMemberAccessExpression).GetConstructor(new[] { typeof(ArmOperation), typeof(IArmString) }),
                new Expression[]
                {
                    Expression.Convert(
                        // Note that here we must use the magic value of originalExpressionMO.Expression
                        // Otherwise we'll get the value for the first metadata object that activates this method forever (it's cached)
                        originalExpressionMO.Expression,
                        typeof(ArmOperation)),
                    Expression.New(
                        typeof(ArmStringLiteral).GetConstructor(new[] { typeof(string) }),
                        new Expression[]
                        {
                            Expression.Property(
                                Expression.Constant(binder),
                                nameof(binder.Name))
                        })
                });

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

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitMemberAccess(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
        {
            return new ArmMemberAccessExpression(
                (ArmOperation)InnerExpression.Instantiate(parameters),
                (IArmString)Member.Instantiate(parameters));
        }
    }
}

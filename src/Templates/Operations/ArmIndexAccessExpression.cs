
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;


namespace PSArm.Templates.Operations
{
    public class ArmIndexAccessExpression : ArmOperation
    {
        internal static DynamicMetaObject CreateMetaObject(
            DynamicMetaObject originalExpressionMO,
            GetIndexBinder binder,
            DynamicMetaObject[] indexes)
        {
            // Generate an expression like this:
            //
            //     new ArmIndexAccessExpression((ArmOperation)dynamicObject, new ArmIntegerLiteral(indexes[0]))
            //
            var expression = Expression.New(
                typeof(ArmIndexAccessExpression).GetConstructor(new[] { typeof(ArmOperation), typeof(ArmExpression) }),
                new Expression[]
                {
                    Expression.Convert(
                        // Note that here we must use the magic value of originalExpressionMO.Expression
                        // Otherwise we'll get the value for the first metadata object that activates this method forever (it's cached)
                        originalExpressionMO.Expression,
                        typeof(ArmOperation)),
                    Expression.New(
                        typeof(ArmIntegerLiteral).GetConstructor(new[] { typeof(long) }),
                        new Expression[]
                        {
                            Expression.Convert(indexes[0].Expression, typeof(long))
                        })
                });


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

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitIndexAccess(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
        {
            return new ArmIndexAccessExpression(
                (ArmOperation)InnerExpression.Instantiate(parameters),
                (ArmExpression)Index.Instantiate(parameters));
        }
    }
}

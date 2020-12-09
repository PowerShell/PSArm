using PSArm.Templates.Primitives;

namespace PSArm.Templates.Operations
{
    public abstract class ArmReferenceExpression<TReference> : ArmFunctionCallExpression where TReference : IArmReferenceable
    {
        protected ArmReferenceExpression(ArmStringValue referenceFunction, TReference referencedValue)
            : base(referenceFunction, new ArmExpression[] { (ArmExpression)referencedValue.ReferenceName })
        {
            ReferencedValue = referencedValue;
        }

        public IArmString ReferenceName => ReferencedValue.ReferenceName;

        public TReference ReferencedValue { get; }
    }
}

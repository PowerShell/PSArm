
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;

namespace PSArm.Templates.Operations
{
    public abstract class ArmReferenceExpression<TReference> : ArmFunctionCallExpression where TReference : IArmReferenceable
    {
        private TReference _referencedValue;

        protected ArmReferenceExpression(ArmStringLiteral referenceFunction, IArmString referencedName)
            : base(referenceFunction, new[] { (ArmExpression)referencedName })
        {
            ReferenceName = referencedName;
        }

        protected ArmReferenceExpression(ArmStringLiteral referenceFunction, TReference referencedValue)
            : this(referenceFunction, referencedValue.ReferenceName)
        {
            ReferencedValue = referencedValue;
        }

        public IArmString ReferenceName { get; private set; }

        public TReference ReferencedValue
        {
            get => _referencedValue;
            set
            {
                ReferenceName = value?.ReferenceName;
                _referencedValue = value;
            }
        }
    }
}

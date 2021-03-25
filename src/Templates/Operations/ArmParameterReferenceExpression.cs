
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Templates.Operations
{
    public class ArmParameterReferenceExpression : ArmReferenceExpression<ArmParameter>
    {
        private static readonly ArmStringLiteral s_parameterReferenceFunction = new ArmStringLiteral("parameters");

        internal ArmParameterReferenceExpression(IArmString parameterName)
            : base(s_parameterReferenceFunction, parameterName)
        {
        }

        public ArmParameterReferenceExpression(ArmParameter parameter)
            : base(s_parameterReferenceFunction, parameter)
        {
        }

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitParameterReference(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
        {
            if (parameters is null)
            {
                return this;
            }

            return parameters.TryGetValue(ReferenceName, out ArmElement value)
                ? value
                : this;
        }
    }
}

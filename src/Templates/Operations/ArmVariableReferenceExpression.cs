
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Templates.Operations
{
    public class ArmVariableReferenceExpression : ArmReferenceExpression<ArmVariable>
    {
        private static readonly ArmStringLiteral s_variableReferenceFunction = new ArmStringLiteral("variables");

        internal ArmVariableReferenceExpression(IArmString variableName)
            : base(s_variableReferenceFunction, variableName)
        {
        }

        public ArmVariableReferenceExpression(ArmVariable variable)
            : base(s_variableReferenceFunction, variable)
        {
        }

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitVariableReference(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => this;
    }
}

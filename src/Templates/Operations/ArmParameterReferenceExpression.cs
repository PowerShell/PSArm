
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System;
using System.Collections.Generic;

namespace PSArm.Templates.Operations
{
    public abstract class ArmParameterReferenceExpression : ArmReferenceExpression<ArmParameter>
    {
        private static readonly ArmStringLiteral s_parameterReferenceFunction = new ArmStringLiteral("parameter");

        protected ArmParameterReferenceExpression(ArmParameter parameter)
            : base(s_parameterReferenceFunction, parameter)
        {
        }

        public abstract Type ParameterType { get; }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitParameterReference(this);
    }

    public class ArmParameterReferenceExpression<TParam> : ArmParameterReferenceExpression
    {
        public ArmParameterReferenceExpression(ArmParameter parameter)
            : base(parameter)
        {
        }

        public override Type ParameterType => typeof(TParam);

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

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System;

namespace PSArm.Templates.Operations
{
    public abstract class ArmParameterReferenceExpression : ArmReferenceExpression<ArmParameter>
    {
        private static readonly ArmStringValue s_parameterReferenceFunction = new ArmStringValue("parameter");

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
    }
}

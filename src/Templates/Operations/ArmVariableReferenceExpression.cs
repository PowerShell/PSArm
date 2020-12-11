using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;

namespace PSArm.Templates.Operations
{
    public class ArmVariableReferenceExpression : ArmReferenceExpression<ArmVariable>
    {
        private static readonly ArmStringLiteral s_variableReferenceFunction = new ArmStringLiteral("variable");

        public ArmVariableReferenceExpression(ArmVariable variable)
            : base(s_variableReferenceFunction, variable)
        {
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitVariableReference(this);
    }
}

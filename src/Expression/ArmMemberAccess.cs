using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    public class ArmMemberAccess : ArmOperation
    {
        public ArmMemberAccess(ArmOperation expression, string member)
        {
            Expression = expression;
            Member = member;
        }

        public ArmOperation Expression { get; }

        public string Member { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmMemberAccess((ArmOperation)Expression.Instantiate(parameters), Member);
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append(Expression.ToInnerExpressionString())
                .Append('.')
                .Append(Member)
                .ToString();
        }
    }

}
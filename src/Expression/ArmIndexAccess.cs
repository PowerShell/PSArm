using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    public class ArmIndexAccess : ArmOperation
    {
        public ArmIndexAccess(ArmOperation expression, int index)
        {
            Expression = expression;
            Index = index;
        }

        public ArmOperation Expression { get; }

        public int Index { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmIndexAccess((ArmOperation)Expression.Instantiate(parameters), Index);
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append(Expression.ToInnerExpressionString())
                .Append('[')
                .Append(Index)
                .Append(']')
                .ToString();
        }
    }

}
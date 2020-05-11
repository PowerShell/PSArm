using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    public class ArmFunctionCall : ArmOperation
    {
        public ArmFunctionCall(string functionName, IArmExpression[] arguments)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }

        public string FunctionName { get; }

        public IArmExpression[] Arguments { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            if (Arguments == null)
            {
                return this;
            }

            var args = new List<IArmExpression>();
            foreach (IArmExpression arg in Arguments)
            {
                args.Add(arg.Instantiate(parameters));
            }

            return new ArmFunctionCall(FunctionName, args.ToArray());
        }

        public override string ToInnerExpressionString()
        {
            var sb = new StringBuilder()
                .Append(FunctionName)
                .Append('(');

            if (Arguments != null && Arguments.Length > 0)
            {
                sb.Append(Arguments[0].ToInnerExpressionString());
                for (int i = 1; i < Arguments.Length; i++)
                {
                    sb.Append(", ")
                        .Append(Arguments[i].ToInnerExpressionString());
                }
            }

            sb.Append(')');
            return sb.ToString();
        }
    }

}
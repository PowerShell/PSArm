using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    public class ArmConcatCall : ArmFunctionCall
    {
        public ArmConcatCall(IArmExpression[] arguments)
            : base("concat", arguments)
        {
        }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            var args = new List<IArmExpression>(Arguments.Length);
            bool canFlatten = true;
            foreach (IArmExpression arg in Arguments)
            {
                IArmExpression resolved = arg.Instantiate(parameters);

                if (!(resolved is ArmStringLiteral))
                {
                    canFlatten = false;
                }

                args.Add(resolved);
            }

            if (canFlatten)
            {
                var sb = new StringBuilder();
                foreach (ArmStringLiteral strArg in args)
                {
                    sb.Append(strArg.Value);
                }
                return new ArmStringLiteral(sb.ToString());
            }

            return new ArmConcatCall(args.ToArray());
        }
    }

}
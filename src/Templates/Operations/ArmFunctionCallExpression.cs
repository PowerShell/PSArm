
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Templates.Operations
{
    public class ArmFunctionCallExpression : ArmOperation
    {
        public ArmFunctionCallExpression()
        {
            Arguments = new List<ArmExpression>();
        }

        public ArmFunctionCallExpression(IArmString function, IReadOnlyList<ArmExpression> arguments)
            : this()
        {
            Function = function;
            Arguments.AddRange(arguments);
        }

        public IArmString Function { get; set; }

        public List<ArmExpression> Arguments { get; }

        public override string ToInnerExpressionString()
        {
            var sb = new StringBuilder();
            sb.Append(Function.ToIdentifierString()).Append('(');

            if (Arguments.Count > 0)
            {
                sb.Append(Arguments[0].ToInnerExpressionString());

                for (int i = 1; i < Arguments.Count; i++)
                {
                    sb.Append(", ").Append(Arguments[i].ToInnerExpressionString());
                }
            }

            sb.Append(')');
            return sb.ToString();
        }

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitFunctionCall(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
        {
            var args = new ArmExpression[Arguments.Count];
            for (int i = 0; i < Arguments.Count; i++)
            {
                args[i] = (ArmExpression)Arguments[i].Instantiate(parameters);
            }

            return new ArmFunctionCallExpression(
                (IArmString)Function.Instantiate(parameters),
                args);
        }
    }
}

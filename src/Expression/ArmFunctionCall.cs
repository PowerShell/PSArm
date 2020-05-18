
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    /// <summary>
    /// Represents an ARM function call like [function()].
    /// </summary>
    public class ArmFunctionCall : ArmOperation
    {
        /// <summary>
        /// Create a function call instance with the given function and arguments.
        /// </summary>
        /// <param name="functionName">The name of the function being called.</param>
        /// <param name="arguments">The arguments to the function.</param>
        public ArmFunctionCall(string functionName, IArmExpression[] arguments)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }

        /// <summary>
        /// The name of the function being called.
        /// </summary>
        public string FunctionName { get; }

        /// <summary>
        /// Arguments to the function.
        /// </summary>
        public IArmExpression[] Arguments { get; }

        /// <summary>
        /// Copy the function call with ARM parameters instantiated.
        /// </summary>
        /// <param name="parameters">The values to instantiate parameters with.</param>
        /// <returns>A copy of the function call expression with ARM parameters instantiated.</returns>
        public override IArmExpression Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
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

        /// <summary>
        /// Render the function call to a string with everything other than the '[', ']' ARM expression terminators,
        /// used for composing expressions.
        /// </summary>
        /// <returns>A string like "function(arg1, arg2, ...)".</returns>
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
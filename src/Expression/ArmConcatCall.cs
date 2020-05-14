using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    /// <summary>
    /// Represents a call to the [concat()] builtin ARM function.
    /// </summary>
    public class ArmConcatCall : ArmFunctionCall
    {
        /// <summary>
        /// Construct a new concat call object.
        /// </summary>
        /// <param name="arguments">That ARM expressions to be concatenated.</param>
        public ArmConcatCall(IArmExpression[] arguments)
            : base("concat", arguments)
        {
        }

        /// <summary>
        /// Instantiate the concat call with the given ARM parameter values.
        /// For concat calls, if all the values are concrete at instantiation time,
        /// we will remove the call entirely and replace it with the final expression instead.
        /// </summary>
        /// <param name="parameters">The values to instantiate parameters with.</param>
        /// <returns>A fully instantiated concat expression, or possibly a constant expression.</returns>
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
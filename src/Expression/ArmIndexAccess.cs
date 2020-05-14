using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    /// <summary>
    /// An ARM array index access expression, like "[variables('thing')[0]]".
    /// </summary>
    public class ArmIndexAccess : ArmOperation
    {
        /// <summary>
        /// Create a new ARM index access expression.
        /// </summary>
        /// <param name="expression">The underlying expression being indexed.</param>
        /// <param name="index">The index itself.</param>
        public ArmIndexAccess(ArmOperation expression, int index)
        {
            Expression = expression;
            Index = index;
        }

        /// <summary>
        /// The underlying expression being indexed.
        /// </summary>
        public ArmOperation Expression { get; }

        /// <summary>
        /// The index to access of the expression.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Copy this expression with ARM parameters instantiated.
        /// </summary>
        /// <param name="parameters">The values to instantiate ARM parameters with.</param>
        /// <returns>A copy of the index expression with ARM parameters instantiated.</returns>
        public override IArmExpression Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            return new ArmIndexAccess((ArmOperation)Expression.Instantiate(parameters), Index);
        }

        /// <summary>
        /// Render the index expression to an ARM expression string without the brackets, for composition.
        /// </summary>
        /// <returns>A string like "expression[index]".</returns>
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
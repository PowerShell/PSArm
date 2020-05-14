using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    /// <summary>
    /// An ARM member access expression.
    /// </summary>
    public class ArmMemberAccess : ArmOperation
    {
        /// <summary>
        /// Create a new ARM member access expression.
        /// </summary>
        /// <param name="expression">The expression whose member is being accessed.</param>
        /// <param name="member">The name of the member being accessed.</param>
        public ArmMemberAccess(ArmOperation expression, string member)
        {
            Expression = expression;
            Member = member;
        }

        /// <summary>
        /// The expression whose member is being accessed.
        /// </summary>
        public ArmOperation Expression { get; }

        /// <summary>
        /// The name of the member being accessed.
        /// </summary>
        public string Member { get; }

        /// <summary>
        /// Copy the member access expression with ARM parameters instantiated.
        /// </summary>
        /// <param name="parameters">The values of ARM parameters to instantiate.</param>
        /// <returns>A copy of the member access expression with parameters instantiated to the given values.</returns>
        public override IArmExpression Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            return new ArmMemberAccess((ArmOperation)Expression.Instantiate(parameters), Member);
        }

        /// <summary>
        /// Render the member access expression as a composable ARM expression string.
        /// </summary>
        /// <returns></returns>
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
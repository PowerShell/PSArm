using System.Collections.Generic;
using System.ComponentModel;

namespace PSArm.Expression
{
    /// <summary>
    /// An ARM expression as used for JSON values in ARM templates.
    /// May be a literal or a more complex expression like a function call or a member access.
    /// </summary>
    [TypeConverter(typeof(ArmTypeConverter))]
    public interface IArmExpression
    {
        /// <summary>
        /// Render this ARM expression to its JSON value.
        /// </summary>
        /// <returns></returns>
        string ToExpressionString();

        /// <summary>
        /// Render the expression to ARM expression syntax in a way that can be composed with other ARM expressions.
        /// </summary>
        /// <returns></returns>
        string ToInnerExpressionString();

        /// <summary>
        /// Copy the ARM expression with any ARM parameters instantiated with given values.
        /// </summary>
        /// <param name="parameters">The values to instantiate parameters with.</param>
        /// <returns></returns>
        IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters);
    }
}
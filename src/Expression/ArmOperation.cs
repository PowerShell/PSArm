using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace PSArm.Expression
{
    /// <summary>
    /// A non-constant ARM expression, such as a function call.
    /// Supports member access magic.
    /// </summary>
    public abstract class ArmOperation : DynamicObject, IArmExpression
    {
        /// <summary>
        /// Render this operation as an ARM expression string.
        /// </summary>
        /// <returns></returns>
        public string ToExpressionString()
        {
            return new StringBuilder()
                .Append('[')
                .Append(ToInnerExpressionString())
                .Append(']')
                .ToString();
        }

        /// <summary>
        /// Render this operation as the inner composable part of an ARM expression string.
        /// </summary>
        /// <returns></returns>
        public abstract string ToInnerExpressionString();

        /// <summary>
        /// Implement dynamic member access on this operation,
        /// so that accessing a member on this object returns a new member access expression object
        /// containing this one plus the accessed member name.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result">A new ARM member access object representing the member access of this expression.</param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new ArmMemberAccess(this, UnPascal(binder.Name));
            return true;
        }

        /// <summary>
        /// Render this expression as an ARM expression string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToExpressionString();

        /// <summary>
        /// Copy this ARM expression with any variables in it instantiated.
        /// </summary>
        /// <param name="parameters">Values to instantiate parameters with.</param>
        /// <returns></returns>
        public abstract IArmExpression Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters);

        private string UnPascal(string s)
        {
            return char.IsUpper(s[0])
                ? char.ToLower(s[0]) + s.Substring(1)
                : s;
        }
    }

}
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace PSArm.Expression
{
    public abstract class ArmOperation : DynamicObject, IArmExpression
    {
        public string ToExpressionString()
        {
            return new StringBuilder()
                .Append('[')
                .Append(ToInnerExpressionString())
                .Append(']')
                .ToString();
        }

        public abstract string ToInnerExpressionString();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new ArmMemberAccess(this, UnPascal(binder.Name));
            return true;
        }

        public override string ToString() => ToExpressionString();

        public abstract IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters);

        private string UnPascal(string s)
        {
            return char.IsUpper(s[0])
                ? char.ToLower(s[0]) + s.Substring(1)
                : s;
        }
    }

}
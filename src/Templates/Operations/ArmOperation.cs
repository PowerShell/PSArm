using Newtonsoft.Json.Linq;
using PSArm.Templates.Primitives;
using System.Collections.Generic;
using System.Dynamic;

namespace PSArm.Templates.Operations
{
    using Expression = System.Linq.Expressions.Expression;

    public abstract class ArmOperation : ArmExpression, IDynamicMetaObjectProvider, IArmString
    {
        public string ToIdentifierString() => ToInnerExpressionString();

        public string ToExpressionString()
        {
            return $"[{ToInnerExpressionString()}]";
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new ArmExpressionMetaObject(parameter, this);
        }

        private class ArmExpressionMetaObject : DynamicMetaObject
        {
            public ArmExpressionMetaObject(Expression expression, ArmOperation value)
                : base(expression, BindingRestrictions.Empty, value)
            {
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                return ArmMemberAccessExpression.CreateMetaObject(this, binder);
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                return ArmIndexAccessExpression.CreateMetaObject(this, binder, indexes);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PsArm
{
    public class ArmExpressionBuilder : DynamicObject
    {
        private readonly ArmExpression _subExpression;

        public ArmExpressionBuilder(ArmExpression subExpression)
        {
            _subExpression = subExpression;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new ArmExpressionBuilder(new ArmLiteralPropertyInvocationExpression(_subExpression, binder.Name));
            return true;
        }

        public ArmExpression GetArmExpression()
        {
            return _subExpression;
        }

        public override string ToString()
        {
            return _subExpression.ToString();
        }
    }

    public abstract class ArmExpression : ArmValue
    {
        internal abstract StringBuilder ToInnerExpressionSyntax();

        public override JToken ToJson()
        {
            return new JValue(new StringBuilder()
                .Append('[')
                .Append(ToInnerExpressionSyntax())
                .Append(']')
                .ToString());
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }

    public class ArmStringLiteralExpression : ArmExpression
    {
        private readonly string _value;

        public ArmStringLiteralExpression(string value)
        {
            _value = value;
        }

        public override JToken ToJson()
        {
            return new JValue(_value);
        }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            return new StringBuilder()
                .Append('\'')
                .Append(_value)
                .Append('\'');
        }
    }

    public class ArmNumberLiteralExpression : ArmExpression
    {
        private readonly decimal _value;

        public ArmNumberLiteralExpression(decimal value)
        {
            _value = value;
        }
        
        public override JToken ToJson()
        {
            return new JValue(_value);
        }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            return new StringBuilder().Append(_value);
        }
    }

    public class ArmFunctionCallExpression : ArmExpression
    {
        private readonly string _functionName;

        private readonly IReadOnlyList<ArmExpression> _parameters;

        public ArmFunctionCallExpression(
            string functionName,
            IReadOnlyCollection<ArmExpression> parameters)
        {
            _functionName = functionName;
            _parameters = parameters.ToArray();
        }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            var sb = new StringBuilder()
                .Append(_functionName)
                .Append("(");

            for (int i = 0; i < _parameters.Count; i++)
            {
                sb.Append(_parameters[i].ToInnerExpressionSyntax());

                if (i != _parameters.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(")");

            return sb;
        }
    }

    public class ArmLiteralPropertyInvocationExpression : ArmExpression
    {
        private readonly ArmExpression _subExpression;

        private readonly string _propertyName;

        public ArmLiteralPropertyInvocationExpression(
            ArmExpression subExpression,
            string propertyName)
        {
            _subExpression = subExpression;
            _propertyName = propertyName;
        }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            return _subExpression.ToInnerExpressionSyntax()
                .Append('.')
                .Append(_propertyName);
        }
    }

    public class ArmParameterizedPropertyInvocationExpression : ArmExpression
    {
        private readonly ArmExpression _objectExpression;

        private readonly ArmExpression _propertyExpression;

        public ArmParameterizedPropertyInvocationExpression(
            ArmExpression objectExpression,
            ArmExpression propertyExpression)
        {
            _objectExpression = objectExpression;
            _propertyExpression = propertyExpression;
        }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            return _objectExpression.ToInnerExpressionSyntax()
                .Append(_propertyExpression.ToJson());
        }
    }

    public class ArmArrayIndexExpression : ArmExpression
    {
        private readonly ArmExpression _subExpression;

        private readonly int _index;

        public ArmArrayIndexExpression(
            ArmExpression subExpression,
            int index)
        {
            _subExpression = subExpression;
            _index = index;
        }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            return _subExpression.ToInnerExpressionSyntax()
                .Append('[')
                .Append(_index)
                .Append(']');
        }
    }
}

using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PSArm.Expression
{
    public class ArmParameter<T> : ArmParameter
    {
        public ArmParameter(string name) : base(name)
        {
            Type = typeof(T);
        }
    }

    public class ArmParameter : ArmOperation
    {
        internal ArmParameter(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Type Type { get; internal set;  }

        public object[] AllowedValues { get; set; }

        public object DefaultValue { get; set; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            ArmLiteral value = parameters[Name];

            if (AllowedValues != null)
            {
                bool found = false;
                foreach (object allowedValue in AllowedValues)
                {
                    if (object.Equals(value.GetValue(), allowedValue))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"Parameter '{Name}' does not have '{value.GetValue()}' as an allowed value");
                }
            }

            return value;
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append("parameters('")
                .Append(Name)
                .Append("')")
                .ToString();
        }

        public JObject ToJson()
        {
            var jObj = new JObject();

            if (Type != null)
            {
                jObj["type"] = GetArmTypeNameFromType(Type);
            }

            if (AllowedValues != null)
            {
                var jArr = new JArray();
                foreach (object val in AllowedValues)
                {
                    jArr.Add(val);
                }
                jObj["allowedValues"] = jArr;
            }

            if (DefaultValue != null)
            {
                jObj["defaultValue"] = DefaultValue is IArmExpression armExpr
                    ? new JValue(armExpr.ToExpressionString())
                    : new JValue(DefaultValue);
            }

            return jObj;
        }

        private string GetArmTypeNameFromType(Type type)
        {
            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(object))
            {
                return "object";
            }

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(SecureString))
            {
                return "securestring";
            }

            if (type == typeof(Array))
            {
                return "array";
            }

            if (type == typeof(SecureObject))
            {
                return "secureObject";
            }

            throw new ArgumentException($"Cannot convert type '{type}' to known ARM type");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PSArm.Expression
{
    public class ArmParameter : ArmOperation
    {
        public ArmParameter(string name)
        {
            Name = name;
        }

        public ArmParameter(string name, string type, object defaultValue)
        {
            this.Name = name;
            this.Type = type;
            this.DefaultValue = defaultValue;

        }
        public string Name { get; }

        public string Type { get; set; }

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
                jObj["type"] = Type;
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
    }
}
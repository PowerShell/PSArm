using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using Newtonsoft.Json.Linq;
using PSArm.ArmBuilding;

namespace PSArm.Expression
{
    /// <summary>
    /// An ARM parameter, parameterized by its ARM type.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    public class ArmParameter<T> : ArmParameter
    {
        /// <summary>
        /// Create a new ARM parameter.
        /// </summary>
        /// <param name="name">The name of the ARM parameter.</param>
        public ArmParameter(string name) : base(name)
        {
            Type = typeof(T);
        }
    }

    /// <summary>
    /// An ARM parameter placeholder within a template,
    /// for later instantiation.
    /// </summary>
    public class ArmParameter : ArmOperation, IArmElement
    {
        public ArmParameter(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the ARM parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the ARM parameter. Must only be a valid ARM parameter type.
        /// </summary>
        public Type Type { get; internal set; }

        /// <summary>
        /// Allowed values for this parameter, if any.
        /// </summary>
        public object[] AllowedValues { get; set; }

        /// <summary>
        /// The default value for this parameter, if any.
        /// </summary>
        public IArmExpression DefaultValue { get; set; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            IArmExpression value = parameters[Name];

            if (value is ArmLiteral literal && AllowedValues != null)
            {
                bool found = false;
                foreach (object allowedValue in AllowedValues)
                {
                    if (object.Equals(literal.GetValue(), allowedValue))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"Parameter '{Name}' does not have '{literal.GetValue()}' as an allowed value");
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

        public JToken ToJson()
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
            if (type == null)
            {
                return null;
            }

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
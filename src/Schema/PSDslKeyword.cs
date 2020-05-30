using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema
{
    public class PSDslKeyword
    {
        public static PSDslKeyword FromSchema(ArmDslKeywordSchema keyword)
        {
            bool hasParameters = false;
            bool hasProperties = false;
            var parameters = new Dictionary<string, PSDslParameterInfo>();

            if (keyword.Parameters != null)
            {
                foreach (ArmDslParameterSchema parameter in keyword.Parameters.Values)
                {
                    hasParameters = true;
                    parameters[GetName(parameter)] = new PSDslParameterInfo(parameter, isProperty: false);
                }
            }

            if (keyword.PropertyParameters != null)
            {
                foreach (ArmDslParameterSchema parameter in keyword.PropertyParameters.Values)
                {
                    hasProperties = true;
                    parameters[GetName(parameter)] = new PSDslParameterInfo(parameter, isProperty: true);
                }
            }

            return new PSDslKeyword(GetName(keyword), parameters, hasParameters, hasProperties);
        }

        public PSDslKeyword(
            string name,
            IReadOnlyDictionary<string, PSDslParameterInfo> parameters,
            bool hasParameters,
            bool hasProperties)
        {
            Name = name;
            Parameters = parameters;
            HasParameters = hasParameters;
            HasProperties = hasProperties;
        }

        public string Name { get; }

        public bool HasParameters { get; }

        public bool HasProperties { get; }

        public IReadOnlyDictionary<string, PSDslParameterInfo> Parameters { get; }

        private static string GetName(ArmDslParameterSchema parameter)
        {
            return Pascal(parameter.Name);
        }

        private static string GetName(ArmDslKeywordSchema keyword)
        {
            string commandName = Pascal(keyword.Name);

            return keyword.Array ? Depluralise(commandName) : commandName;
        }

        private static string Pascal(string s)
        {
            return char.IsUpper(s[0])
                ? s
                : char.ToUpper(s[0]) + s.Substring(1);
        }

        private static string Depluralise(string s)
        {
            int i = s.LastIndexOf("ies");
            if (i > 0)
            {
                return s.Substring(0, i) + "y";
            }

            i = s.LastIndexOf("s");
            if (i > 0)
            {
                return s.Substring(0, i);
            }

            return s;
        }
    }

    public class PSDslParameterInfo
    {
        public PSDslParameterInfo(ArmDslParameterSchema parameter, bool isProperty)
        {
            Parameter = parameter;
            IsPropertyParameter = isProperty;
        }

        public bool IsPropertyParameter { get; }

        public ArmDslParameterSchema Parameter { get; }
    }
}

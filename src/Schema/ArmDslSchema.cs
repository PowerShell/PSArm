using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema
{
    public abstract class ArmDslSchema
    {
        protected ArmDslSchema()
        {
        }
    }

    public class ArmDslProviderSchema : ArmDslSchema
    {
        public ArmDslProviderSchema(string providerName, string apiVersion)
        {
            ProviderName = providerName;
            ApiVersion = apiVersion;
            Resources = new Dictionary<string, ArmDslResourceSchema>();
        }

        public string ProviderName { get; }

        public string ApiVersion { get; }

        public Dictionary<string, ArmDslKeywordSchema> Keywords { get; set; }

        public Dictionary<string, ArmDslResourceSchema> Resources { get; }
    }

    public class ArmDslKeywordSchema : ArmDslSchema
    {
        public ArmDslKeywordSchema(string name)
        {
            Name = name;
            Parameters = new Dictionary<string, ArmDslParameterSchema>();
            PropertyParameters = new Dictionary<string, ArmDslParameterSchema>();
        }

        public string Name { get; }

        public Dictionary<string, ArmDslParameterSchema> Parameters { get; }

        public Dictionary<string, ArmDslParameterSchema> PropertyParameters { get; }

        public bool Array { get; set; } = false;

        public Dictionary<string, ArmDslKeywordSchema> Body { get; set; }
    }

    public class ArmDslParameterSchema : ArmDslSchema
    {
        public ArmDslParameterSchema(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<object> Enum { get; set; }

        public string Type { get; set; }
    }

    public class ArmDslResourceSchema : ArmDslSchema
    {
        public ArmDslResourceSchema(string resourceType)
        {
            ResourceType = resourceType;
            Keywords = new Dictionary<string, ArmDslKeywordSchema>();
        }

        public string ResourceType { get; }

        public Dictionary<string, ArmDslKeywordSchema> Keywords { get; }
    }
}


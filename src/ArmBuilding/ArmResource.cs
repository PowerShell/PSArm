using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmResource : IArmElement
    {
        public string ApiVersion { get; set; }

        public string Type { get; set; }

        public IArmExpression Name { get; set; }

        public IArmExpression Location { get; set; }

        public IArmExpression Kind { get; set; }

        public ArmSku Sku { get; set; }

        public Dictionary<string, ArmPropertyInstance> Properties { get; set; }

        public Dictionary<IArmExpression, ArmResource> Subresources { get; set; }

        public List<IArmExpression> DependsOn { get; set; }

        public JToken ToJson()
        {
            var jObj = new JObject
            {
                ["apiVersion"] = ApiVersion,
                ["type"] = Type,
                ["name"] = Name.ToExpressionString(),
                ["location"] = Location.ToExpressionString(),
            };

            var properties = new JObject();
            foreach (KeyValuePair<string, ArmPropertyInstance> property in Properties)
            {
                properties[property.Key] = property.Value.ToJson();
            }
            jObj["properties"] = properties;

            if (Subresources != null && Subresources.Count > 0)
            {
                var subresources = new JArray();
                foreach (KeyValuePair<IArmExpression, ArmResource> subresource in Subresources)
                {
                    subresources.Add(subresource.Value.ToJson());
                }
                jObj["resources"] = subresources;
            }

            if (DependsOn != null && DependsOn.Count > 0)
            {
                var dependsOn = new JArray();
                foreach (IArmExpression dependency in DependsOn)
                {
                    dependsOn.Add(dependency.ToExpressionString());
                }
                jObj["dependsOn"] = dependsOn;
            }

            if (Kind != null)
            {
                jObj["kind"] = Kind.ToExpressionString();
            }

            if (Sku != null)
            {
                jObj["sku"] = Sku.ToJson();
            }

            return jObj;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }

        public ArmResource Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            Dictionary<string, ArmPropertyInstance> properties = null;
            if (Properties != null)
            {
                properties = new Dictionary<string, ArmPropertyInstance>();
                foreach (KeyValuePair<string, ArmPropertyInstance> property in Properties)
                {
                    properties[property.Key] = property.Value.Instantiate(parameters);
                }
            }

            Dictionary<IArmExpression, ArmResource> subResources = null;
            if (Subresources != null)
            {
                subResources = new Dictionary<IArmExpression, ArmResource>();
                foreach (KeyValuePair<IArmExpression, ArmResource> resource in Subresources)
                {
                    subResources[resource.Key.Instantiate(parameters)] = resource.Value.Instantiate(parameters);
                }
            }

            List<IArmExpression> dependsOn = null;
            if (DependsOn != null)
            {
                dependsOn = new List<IArmExpression>();
                foreach (IArmExpression dependency in DependsOn)
                {
                    dependsOn.Add(dependency.Instantiate(parameters));
                }
            }

            return new ArmResource
            {
                ApiVersion = ApiVersion,
                Type = Type,
                Name = Name.Instantiate(parameters),
                Location = Location?.Instantiate(parameters),
                Properties = properties,
                Subresources = subResources,
                Kind = Kind?.Instantiate(parameters),
                Sku = Sku?.Instantiate(parameters),
                DependsOn = dependsOn,
            };
        }
    }
}
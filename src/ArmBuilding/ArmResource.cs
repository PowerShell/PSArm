
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// An ARM template resource instance.
    /// </summary>
    public class ArmResource : IArmElement
    {
        /// <summary>
        /// The API version specified for this resource. May be null.
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// The resource type specified.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The name of this ARM resource.
        /// </summary>
        public IArmExpression Name { get; set; }

        /// <summary>
        /// The Azure region or location of this ARM resource.
        /// </summary>
        public IArmExpression Location { get; set; }

        /// <summary>
        /// The kind field of the ARM resource, with a resource-specific meaning.
        /// </summary>
        public IArmExpression Kind { get; set; }

        /// <summary>
        /// The SKU of the resource, which may not apply to all resources.
        /// </summary>
        public ArmSku Sku { get; set; }

        /// <summary>
        /// Any properties of the resource.
        /// </summary>
        public Dictionary<string, ArmPropertyInstance> Properties { get; set; }

        /// <summary>
        /// Any subresources of this resource.
        /// </summary>
        public Dictionary<IArmExpression, ArmResource> Subresources { get; set; }

        /// <summary>
        /// The other resources this resource depends on.
        /// </summary>
        public List<IArmExpression> DependsOn { get; set; }

        /// <summary>
        /// Render this resource as ARM template JSON.
        /// </summary>
        /// <returns>A JSON object representation of the ARM template JSON expression of this resource.</returns>
        public JToken ToJson()
        {
            var jObj = new JObject
            {
                ["apiVersion"] = ApiVersion,
                ["type"] = Type,
                ["name"] = Name?.ToExpressionString(),
                ["location"] = Location?.ToExpressionString(),
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

        /// <summary>
        /// Get an ARM template JSON string representation of this resource.
        /// </summary>
        /// <returns>A JSON string representing this ARM resource in ARM template JSON.</returns>
        public override string ToString()
        {
            return ToJson().ToString();
        }

        /// <summary>
        /// Instantiate all ARM parameters in this resource with the given values.
        /// </summary>
        /// <param name="parameters">The values to instantiate ARM parameters with.</param>
        /// <returns>A copy of the resource with parameter values instantiated.</returns>
        public ArmResource Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
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
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PSArm
{
    public abstract class ArmPropertyInstance
    {
        public ArmPropertyInstance(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        public abstract JToken ToJson();

        public override string ToString()
        {
            return ToJson().ToString();
        }

        public abstract ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters);
    }

    public class ArmPropertyValue : ArmPropertyInstance
    {
        public ArmPropertyValue(string propertyName, IArmExpression value)
            : base(propertyName)
        {
            Value = value;
        }

        public IArmExpression Value { get; }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmPropertyValue(PropertyName, Value.Instantiate(parameters));
        }

        public override JToken ToJson()
        {
            return Value.ToExpressionString();
        }
    }

    public abstract class ArmParameterizedItem : ArmPropertyInstance
    {
        public ArmParameterizedItem(string propertyName)
            : base(propertyName)
        {
            Parameters = new Dictionary<string, IArmExpression>();
        }

        public Dictionary<string, IArmExpression> Parameters { get; protected set; }

        protected Dictionary<string, IArmExpression> InstantiateParameters(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            if (Parameters == null)
            {
                return null;
            }

            var dict = new Dictionary<string, IArmExpression>();
            foreach (KeyValuePair<string, IArmExpression> parameter in Parameters)
            {
                dict[parameter.Key] = parameter.Value.Instantiate(parameters);
            }
            return dict;
        }
    }

    public class ArmParameterizedProperty : ArmParameterizedItem
    {
        public ArmParameterizedProperty(string propertyName)
            : base(propertyName)
        {
        }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmParameterizedProperty(PropertyName)
            {
                Parameters = InstantiateParameters(parameters),
            };
        }

        public override JToken ToJson()
        {
            var jObj = new JObject();
            foreach (KeyValuePair<string, IArmExpression> parameter in Parameters)
            {
                jObj[parameter.Key] = parameter.Value.ToExpressionString();
            }
            return jObj;
        }
    }

    public class ArmPropertyObject : ArmParameterizedItem
    {
        public ArmPropertyObject(string propertyName)
            : this(propertyName, new Dictionary<string, ArmPropertyInstance>())
        {
        }

        internal ArmPropertyObject(string propertyName, Dictionary<string, ArmPropertyInstance> properties)
            : base(propertyName)
        {
            Properties = properties;
        }

        public Dictionary<string, ArmPropertyInstance> Properties { get; }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmPropertyObject(PropertyName, InstantiateProperties(parameters))
            {
                Parameters = InstantiateParameters(parameters),
            };
        }

        public override JToken ToJson()
        {
            var json = new JObject();
            foreach (KeyValuePair<string, IArmExpression> parameter in Parameters)
            {
                json[parameter.Key] = parameter.Value.ToExpressionString();
            }

            var properties = new JObject();
            foreach (KeyValuePair<string, ArmPropertyInstance> property in Properties)
            {
                properties[property.Key] = property.Value.ToJson();
            }
            json["properties"] = properties;

            return json;
        }

        protected Dictionary<string, ArmPropertyInstance> InstantiateProperties(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            if (Properties == null)
            {
                return null;
            }

            var dict = new Dictionary<string, ArmPropertyInstance>();
            foreach (KeyValuePair<string, ArmPropertyInstance> property in Properties)
            {
                dict[property.Key] = property.Value.Instantiate(parameters);
            }
            return dict;
        }
    }

    public class ArmPropertyArrayItem : ArmPropertyObject
    {
        public ArmPropertyArrayItem(string propertyName) : base(propertyName)
        {
        }

        public ArmPropertyArrayItem(string propertyName, Dictionary<string, ArmPropertyInstance> properties)
            : base(propertyName, properties)
        {
        }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmPropertyArrayItem(PropertyName, InstantiateProperties(parameters))
            {
                Parameters = InstantiateParameters(parameters),
            };
        }
    }

    internal class ArmPropertyArray : ArmPropertyInstance
    {
        public static ArmPropertyArray FromArrayItems(List<ArmPropertyArrayItem> items)
        {
            string name = items[0].PropertyName + "s";
            return new ArmPropertyArray(name, items);
        }

        private readonly List<ArmPropertyArrayItem> _items;

        private ArmPropertyArray(string propertyName, List<ArmPropertyArrayItem> items) : base(propertyName)
        {
            _items = items;
        }

        public override JToken ToJson()
        {
            var jArr = new JArray();
            foreach (ArmPropertyArrayItem item in _items)
            {
                jArr.Add(item.ToJson());
            }
            return jArr;
        }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            var items = new List<ArmPropertyArrayItem>();
            foreach (ArmPropertyArrayItem item in _items)
            {
                items.Add((ArmPropertyArrayItem)item.Instantiate(parameters));
            }
            return new ArmPropertyArray(PropertyName, items);
        }
    }

    public class ArmDependsOn
    {
        public ArmDependsOn(IArmExpression value)
        {
            Value = value;
        }

        public IArmExpression Value { get; }
    }

    public class ArmResource
    {
        public string ApiVersion { get; set; }

        public string Type { get; set; }

        public IArmExpression Name { get; set; }

        public IArmExpression Location { get; set; }

        public Dictionary<string, ArmPropertyInstance> Properties { get; set; }

        public Dictionary<IArmExpression, ArmResource> Subresources { get; set; }

        public List<IArmExpression> DependsOn { get; set; }

        public JObject ToJson()
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

            if (Subresources != null)
            {
                var subresources = new JArray();
                foreach (KeyValuePair<IArmExpression, ArmResource> subresource in Subresources)
                {
                    subresources.Add(subresource.Value.ToJson());
                }
                jObj["resources"] = subresources;
            }

            if (DependsOn != null)
            {
                var dependsOn = new JArray();
                foreach (IArmExpression dependency in DependsOn)
                {
                    dependsOn.Add(dependency.ToExpressionString());
                }
                jObj["dependsOn"] = dependsOn;
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

            return new ArmResource
            {
                ApiVersion = ApiVersion,
                Type = Type,
                Name = Name.Instantiate(parameters),
                Location = Location.Instantiate(parameters),
                Properties = properties,
                Subresources = subResources,
            };
        }
    }

    public class ArmOutput
    {
        public IArmExpression Name { get; set; }

        public IArmExpression Type { get; set; }

        public IArmExpression Value { get; set; }

        public JToken ToJson()
        {
            return new JObject
            {
                ["type"] = Type.ToExpressionString(),
                ["value"] = Value.ToExpressionString(),
            };
        }

        public ArmOutput Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmOutput
            {
                Name = Name.Instantiate(parameters),
                Type = Type.Instantiate(parameters),
                Value = Value.Instantiate(parameters),
            };
        }
    }

    public class ArmTemplate
    {
        public ArmTemplate()
        {
            Resources = new List<ArmResource>();
            Outputs = new List<ArmOutput>();
        }

        public string Schema { get; set; } = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#";

        public Version ContentVersion { get; set; } = new Version(1, 0, 0, 0);

        public List<ArmResource> Resources { get; set; }

        public List<ArmOutput> Outputs { get; set; }

        public ArmParameter[] Parameters { get; set; }

        public JObject ToJson()
        {
            var jObj = new JObject
            {
                ["$schema"] = Schema,
                ["contentVersion"] = ContentVersion.ToString(),
            };

            if (Parameters != null)
            {
                var parameters = new JObject();
                foreach (ArmParameter parameter in Parameters)
                {
                    parameters[parameter.Name] = parameter.ToJson();
                }
                jObj["parameters"] = parameters;
            }

            var outputs = new JObject();
            foreach (ArmOutput output in Outputs)
            {
                outputs[output.Name.ToExpressionString()] = output.ToJson();
            }
            jObj["outputs"] = outputs;

            var resources = new JArray();
            foreach (ArmResource resource in Resources)
            {
                resources.Add(resource.ToJson());
            }
            jObj["resources"] = resources;

            return jObj;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }

        public ArmTemplate Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            var outputs = new List<ArmOutput>();
            foreach (ArmOutput output in Outputs)
            {
                outputs.Add(output.Instantiate(parameters));
            }

            var resources = new List<ArmResource>();
            foreach (ArmResource resource in Resources)
            {
                resources.Add(resource.Instantiate(parameters));
            }

            return new ArmTemplate
            {
                ContentVersion = ContentVersion,
                Schema = Schema,
                Outputs = outputs,
                Resources = resources,
            };
        }
    }
}
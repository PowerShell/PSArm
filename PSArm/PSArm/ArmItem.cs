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
    }

    public class ArmPropertyValue : ArmPropertyInstance
    {
        public ArmPropertyValue(string propertyName, object value)
            : base(propertyName)
        {
            Value = value;
        }

        public object Value { get; }

        public override JToken ToJson()
        {
            return new JValue(Value);
        }
    }

    public abstract class ArmParameterizedItem : ArmPropertyInstance
    {
        public ArmParameterizedItem(string propertyName)
            : base(propertyName)
        {
            Parameters = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Parameters { get; }
    }

    public class ArmParameterizedProperty : ArmParameterizedItem
    {
        public ArmParameterizedProperty(string propertyName)
            : base(propertyName)
        {
        }

        public override JToken ToJson()
        {
            var jObj = new JObject();
            foreach (KeyValuePair<string, object> parameter in Parameters)
            {
                jObj[parameter.Key] = new JValue(parameter.Value);
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

        public override JToken ToJson()
        {
            var json = new JObject();
            foreach (KeyValuePair<string, object> parameter in Parameters)
            {
                json[parameter.Key] = new JValue(parameter.Value);
            }

            var properties = new JObject();
            foreach (KeyValuePair<string, ArmPropertyInstance> property in Properties)
            {
                properties[property.Key] = property.Value.ToJson();
            }
            json["properties"] = properties;

            return json;
        }
    }

    public class ArmPropertyArrayItem : ArmPropertyObject
    {
        public ArmPropertyArrayItem(string propertyName) : base(propertyName)
        {
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
    }

    public class ArmResource
    {
        public string ApiVersion { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public Dictionary<string, ArmPropertyInstance> Properties { get; set; }

        public Dictionary<string, ArmResource> Subresources { get; set; }

        public JObject ToJson()
        {
            var jObj = new JObject
            {
                ["apiVersion"] = new JValue(ApiVersion),
                ["type"] = new JValue(Type),
                ["name"] = new JValue(Name),
                ["location"] = new JValue(Location),
            };

            var properties = new JObject();
            foreach (KeyValuePair<string, ArmPropertyInstance> property in Properties)
            {
                properties[property.Key] = property.Value.ToJson();
            }
            jObj["properties"] = properties;

            return jObj;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}
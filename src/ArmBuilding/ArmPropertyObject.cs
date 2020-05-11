using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
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
}

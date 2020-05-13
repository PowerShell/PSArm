using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// An ARM property element with an object body.
    /// </summary>
    public class ArmPropertyObject : ArmParameterizedItem
    {
        /// <summary>
        /// Create a new ARM property object with the given property name.
        /// </summary>
        /// <param name="propertyName">The property name under which the parent element will place this element.</param>
        public ArmPropertyObject(string propertyName)
            : this(propertyName, new Dictionary<string, ArmPropertyInstance>())
        {
        }

        /// <summary>
        /// Create a new ARM property object.
        /// </summary>
        /// <param name="propertyName">The property name under which the parent element will place this element.</param>
        /// <param name="properties">A dictionary of subproperties of this property.</param>
        internal ArmPropertyObject(string propertyName, Dictionary<string, ArmPropertyInstance> properties)
            : base(propertyName)
        {
            Properties = properties;
        }

        /// <summary>
        /// All subproperties of this property, keyed by property name.
        /// </summary>
        public Dictionary<string, ArmPropertyInstance> Properties { get; }

        /// <summary>
        /// Instantiate all ARM parameters in this element with the given parameter values.
        /// </summary>
        /// <param name="parameters">The parameter values to use for instantiation, keyed by parameter name.</param>
        /// <returns>A copy of the property element with parameter values instantiated.</returns>
        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmPropertyObject(PropertyName, InstantiateProperties(parameters))
            {
                Parameters = InstantiateParameters(parameters),
            };
        }

        /// <summary>
        /// Render this ARM element as a JSON object.
        /// </summary>
        /// <returns>The JSON object representation of this ARM element.</returns>
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

        /// <summary>
        /// Instantiate ARM parameters in the properties of this element.
        /// </summary>
        /// <param name="parameters">The parameter values to use for instantiation.</param>
        /// <returns>Copies of all properties with ARM parameters instantiated.</returns>
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

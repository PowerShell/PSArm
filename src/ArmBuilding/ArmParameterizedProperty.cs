
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// An ARM property with a parameters field.
    /// </summary>
    public class ArmParameterizedProperty : ArmParameterizedItem
    {
        /// <summary>
        /// Create a new ARM property with a given name.
        /// </summary>
        /// <param name="propertyName">The name of the property this element is keyed by in its parent.</param>
        public ArmParameterizedProperty(string propertyName)
            : base(propertyName)
        {
        }

        /// <summary>
        /// Instantiate any ARM parameters with the given literal values.
        /// </summary>
        /// <param name="parameters">The literal parameter values to instantiate with.</param>
        /// <returns>The ARM property instance with parameters instantiated.</returns>
        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, IArmValue> parameters)
        {
            return new ArmParameterizedProperty(PropertyName)
            {
                Parameters = InstantiateParameters(parameters),
            };
        }

        /// <summary>
        /// Render the ARM property instance as an ARM template JSON object.
        /// </summary>
        /// <returns>The ARM property instance in ARM template JSON form.</returns>
        public override JToken ToJson()
        {
            var jObj = new JObject();
            foreach (KeyValuePair<string, IArmValue> parameter in Parameters)
            {
                if (parameter.Value != null)
                {
                    jObj[parameter.Key] = parameter.Value.ToJson();
                }
            }
            return jObj;
        }
    }
}


// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// An ARM JSON element paired with its JSON field name.
    /// </summary>
    public abstract class ArmPropertyInstance : IArmElement
    {
        /// <summary>
        /// Create a new ARM property element.
        /// </summary>
        /// <param name="propertyName">The field name that this element will appear under in its parent.</param>
        protected ArmPropertyInstance(string propertyName)
        {
            PropertyName = propertyName;
        }

        /// <summary>
        /// The property name this element will take in its JSON parent.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Render this ARM element as ARM template JSON.
        /// This should produce the JSON for the body of the element, not the property name;
        /// the invoking parent has the responsibility of deriving the name correctly.
        /// </summary>
        /// <returns>A JSON object representing the ARM template JSON of this element.</returns>
        public abstract JToken ToJson();

        /// <summary>
        /// Render this ARM element as a string.
        /// </summary>
        /// <returns>The JSON string of this ARM element's body.</returns>
        public override string ToString()
        {
            return ToJson().ToString();
        }

        /// <summary>
        /// Instantiate all ARM parameters used in this element with the given values.
        /// </summary>
        /// <param name="parameters">The parameter values to use for instantiation.</param>
        /// <returns>A copy of the original element with ARM parameters instantiated with the given values.</returns>
        public abstract ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, IArmValue> parameters);
    }
}

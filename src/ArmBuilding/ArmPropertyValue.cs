
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// A simple, value-bodied ARM JSON property.
    /// </summary>
    public class ArmPropertyValue : ArmPropertyInstance
    {
        /// <summary>
        /// Create an ARM property value element.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        public ArmPropertyValue(string propertyName, IArmExpression value)
            : base(propertyName)
        {
            Value = value;
        }

        /// <summary>
        /// The value of the ARM element.
        /// </summary>
        public IArmExpression Value { get; }

        /// <summary>
        /// Instantiate ARM parameters within this element with the given values.
        /// </summary>
        /// <param name="parameters">The values with which to instantiate ARM parameters.</param>
        /// <returns>A copy of the property element with the parameters instantiated.</returns>
        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            return new ArmPropertyValue(PropertyName, Value.Instantiate(parameters));
        }

        /// <summary>
        /// Render this ARM property element's body as a JSON object.
        /// </summary>
        /// <returns>A JSON object representing the ARM template JSON of this element.</returns>
        public override JToken ToJson()
        {
            return Value.ToExpressionString();
        }
    }
}

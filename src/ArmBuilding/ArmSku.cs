
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// Represents the SKU property on an ARM resource.
    /// </summary>
    public class ArmSku : IArmElement
    {
        /// <summary>
        /// The name of the SKU.
        /// </summary>
        public IArmExpression Name { get; set; }

        public IArmExpression Tier { get; set; }

        public IArmExpression Size { get; set; }

        public IArmExpression Family { get; set; }

        public IArmExpression Capacity { get; set; }

        /// <summary>
        /// Instantiate the ARM parameters in this SKU with the given values.
        /// </summary>
        /// <param name="parameters">The parameter values to instantiate with.</param>
        /// <returns>A copy of this SKU with ARM parameters instantiated.</returns>
        public ArmSku Instantiate(IReadOnlyDictionary<string, IArmValue> parameters)
        {
            return new ArmSku
            {
                Name = (IArmExpression)Name.Instantiate(parameters),
            };
        }

        /// <summary>
        /// Render this SKU as a JSON object.
        /// </summary>
        /// <returns>A JSON object representing the ARM template JSON form of this SKU.</returns>
        public JToken ToJson()
        {
            return new JObject
            {
                ["name"] = Name.ToExpressionString(),
            };
        }
    }

}
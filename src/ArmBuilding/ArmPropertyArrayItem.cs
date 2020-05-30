
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// An ARM property that forms an element in an ARM array property,
    /// so that multiple ARM property array items with the same name
    /// will be aggregated into a single array property.
    /// </summary>
    public class ArmPropertyArrayItem : ArmPropertyObject
    {
        /// <summary>
        /// Construct a new ARM property array item.
        /// </summary>
        /// <param name="propertyName">The field name the property has in its parent.</param>
        public ArmPropertyArrayItem(string propertyName) : base(propertyName)
        {
        }

        /// <summary>
        /// Construct a new ARM property array item with the given subproperties.
        /// </summary>
        /// <param name="propertyName">The field name the property has in its parent.</param>
        /// <param name="properties">The dictionary of subproperties of this item.</param>
        public ArmPropertyArrayItem(string propertyName, Dictionary<string, ArmPropertyInstance> properties)
            : base(propertyName, properties)
        {
        }

        /// <summary>
        /// Instantiate all ARM parameters in this item with the given values.
        /// </summary>
        /// <param name="parameters">The values to instantiate parameters with.</param>
        /// <returns>A copy of this element with ARM parameters instantiated.</returns>
        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            return new ArmPropertyArrayItem(PropertyName, InstantiateProperties(parameters))
            {
                Parameters = InstantiateParameters(parameters),
            };
        }
    }
}

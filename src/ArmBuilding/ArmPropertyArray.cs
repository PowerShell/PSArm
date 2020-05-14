using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// An ARM property that has an array of child elements.
    /// Only intended to aggregate ArmPropertyArrayItems.
    /// </summary>
    internal class ArmPropertyArray : ArmPropertyInstance
    {
        /// <summary>
        /// Create an ARM property array from a list of array items.
        /// </summary>
        /// <param name="items">The items to aggregrate.</param>
        /// <returns>The items as an aggregated ARM property array object.</returns>
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

        /// <summary>
        /// Render the ARM property array body as ARM template JSON.
        /// </summary>
        /// <returns>The JSON representation of the property array.</returns>
        public override JToken ToJson()
        {
            var jArr = new JArray();
            foreach (ArmPropertyArrayItem item in _items)
            {
                jArr.Add(item.ToJson());
            }
            return jArr;
        }

        /// <summary>
        /// Instantiate any ARM parameters in this property with the given values.
        /// </summary>
        /// <param name="parameters">The values to instantiate with.</param>
        /// <returns>A copy of the ARM property with the parameter values instantiated.</returns>
        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            var items = new List<ArmPropertyArrayItem>();
            foreach (ArmPropertyArrayItem item in _items)
            {
                items.Add((ArmPropertyArrayItem)item.Instantiate(parameters));
            }
            return new ArmPropertyArray(PropertyName, items);
        }
    }
}

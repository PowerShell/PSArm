using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
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
}

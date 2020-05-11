using System.Collections.Generic;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmPropertyArrayItem : ArmPropertyObject
    {
        public ArmPropertyArrayItem(string propertyName) : base(propertyName)
        {
        }

        public ArmPropertyArrayItem(string propertyName, Dictionary<string, ArmPropertyInstance> properties)
            : base(propertyName, properties)
        {
        }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmPropertyArrayItem(PropertyName, InstantiateProperties(parameters))
            {
                Parameters = InstantiateParameters(parameters),
            };
        }
    }
}

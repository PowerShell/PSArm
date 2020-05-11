using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public abstract class ArmPropertyInstance
    {
        public ArmPropertyInstance(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        public abstract JToken ToJson();

        public override string ToString()
        {
            return ToJson().ToString();
        }

        public abstract ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters);
    }
}

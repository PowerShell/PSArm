using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmPropertyValue : ArmPropertyInstance
    {
        public ArmPropertyValue(string propertyName, IArmExpression value)
            : base(propertyName)
        {
            Value = value;
        }

        public IArmExpression Value { get; }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmPropertyValue(PropertyName, Value.Instantiate(parameters));
        }

        public override JToken ToJson()
        {
            return Value.ToExpressionString();
        }
    }
}

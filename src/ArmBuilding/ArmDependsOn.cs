using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmDependsOn : IArmElement
    {
        public ArmDependsOn(IArmExpression value)
        {
            Value = value;
        }

        public IArmExpression Value { get; }

        public ArmDependsOn Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmDependsOn(Value.Instantiate(parameters));
        }

        public JToken ToJson()
        {
            return new JValue(Value.ToExpressionString());
        }
    }
}

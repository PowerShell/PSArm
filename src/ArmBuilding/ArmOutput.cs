using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmOutput
    {
        public IArmExpression Name { get; set; }

        public IArmExpression Type { get; set; }

        public IArmExpression Value { get; set; }

        public JToken ToJson()
        {
            return new JObject
            {
                ["type"] = Type.ToExpressionString(),
                ["value"] = Value.ToExpressionString(),
            };
        }

        public ArmOutput Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmOutput
            {
                Name = Name.Instantiate(parameters),
                Type = Type.Instantiate(parameters),
                Value = Value.Instantiate(parameters),
            };
        }
    }
}

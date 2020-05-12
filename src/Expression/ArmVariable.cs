using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using PSArm.ArmBuilding;

namespace PSArm.Expression
{
    public class ArmVariable : ArmOperation, IArmElement
    {
        public ArmVariable(string name, IArmExpression value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public IArmExpression Value { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmVariable(Name, Value.Instantiate(parameters));
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append("variables('")
                .Append(Name)
                .Append("')")
                .ToString();
        }

        public JToken ToJson()
        {
            return new JValue(Value.ToExpressionString());
        }
    }

}
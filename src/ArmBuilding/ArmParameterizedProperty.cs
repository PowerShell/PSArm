using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmParameterizedProperty : ArmParameterizedItem
    {
        public ArmParameterizedProperty(string propertyName)
            : base(propertyName)
        {
        }

        public override ArmPropertyInstance Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmParameterizedProperty(PropertyName)
            {
                Parameters = InstantiateParameters(parameters),
            };
        }

        public override JToken ToJson()
        {
            var jObj = new JObject();
            foreach (KeyValuePair<string, IArmExpression> parameter in Parameters)
            {
                jObj[parameter.Key] = parameter.Value.ToExpressionString();
            }
            return jObj;
        }
    }
}

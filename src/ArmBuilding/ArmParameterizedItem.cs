using System.Collections.Generic;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public abstract class ArmParameterizedItem : ArmPropertyInstance
    {
        public ArmParameterizedItem(string propertyName)
            : base(propertyName)
        {
            Parameters = new Dictionary<string, IArmExpression>();
        }

        public Dictionary<string, IArmExpression> Parameters { get; protected set; }

        protected Dictionary<string, IArmExpression> InstantiateParameters(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            if (Parameters == null)
            {
                return null;
            }

            var dict = new Dictionary<string, IArmExpression>();
            foreach (KeyValuePair<string, IArmExpression> parameter in Parameters)
            {
                dict[parameter.Key] = parameter.Value.Instantiate(parameters);
            }
            return dict;
        }
    }
}
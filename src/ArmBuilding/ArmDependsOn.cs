using System.Collections.Generic;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmDependsOn
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
    }
}

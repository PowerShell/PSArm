using System.Collections.Generic;
using System.ComponentModel;

namespace PSArm.Expression
{
    [TypeConverter(typeof(ArmTypeConverter))]
    public interface IArmExpression
    {
        string ToExpressionString();

        string ToInnerExpressionString();

        IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters);
    }
}
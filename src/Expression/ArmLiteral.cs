using System.Collections.Generic;
using System.ComponentModel;

namespace PSArm.Expression
{
    [TypeConverter(typeof(ArmTypeConverter))]
    public abstract class ArmLiteral : IArmExpression
    {
        public IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters) => this;

        public abstract string ToExpressionString();

        public abstract string ToInnerExpressionString();

        public override string ToString() => ToExpressionString();

        public abstract object GetValue();
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public abstract class ArmLiteral<T> : ArmLiteral
    {
        public ArmLiteral(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public override object GetValue() => Value;
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmStringLiteral : ArmLiteral<string>
    {
        public ArmStringLiteral(string value) : base(value)
        {
        }

        public override string ToExpressionString()
        {
            return Value.StartsWith("[") && Value.EndsWith("]")
                ? "[" + Value
                : Value;
        }

        public override string ToInnerExpressionString()
        {
            return "'" + Value + "'";
        }
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmIntLiteral : ArmLiteral<int>
    {
        public ArmIntLiteral(int value) : base(value)
        {
        }

        public override string ToExpressionString() => Value.ToString();

        public override string ToInnerExpressionString() => ToExpressionString();
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmBoolLiteral : ArmLiteral<bool>
    {
        public ArmBoolLiteral(bool value) : base(value)
        {
        }

        public override string ToExpressionString()
        {
            return Value
                ? "true"
                : "false";
        }

        public override string ToInnerExpressionString() => ToExpressionString();
    }

}
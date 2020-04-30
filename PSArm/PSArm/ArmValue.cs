namespace PSArm
{
    public abstract class ArmValue
    {
        public abstract string ToExpressionString();

        public override string ToString() => ToExpressionString();
    }

    public abstract class ArmLiteralValue<T> : ArmValue
    {
        public ArmLiteralValue(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public override string ToExpressionString()
        {
            return Value.ToString();
        }
    }

    public class ArmStringLiteral : ArmLiteralValue<string>
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
    }

    public class ArmIntLiteral : ArmLiteralValue<int>
    {
        public ArmIntLiteral(int value) : base(value)
        {
        }
    }

    public class ArmBoolLiteral : ArmLiteralValue<bool>
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
    }
}
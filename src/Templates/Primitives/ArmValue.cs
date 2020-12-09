using Newtonsoft.Json.Linq;
using PSArm.Types;

namespace PSArm.Templates.Primitives
{

    public abstract class ArmValue : ArmExpression
    {
        private readonly object _value;

        protected ArmValue(object value, ArmType armType)
        {
            _value = value;
            ArmType = armType;
        }

        public ArmType ArmType { get; }

        public object GetValue() => _value;
    }

    public abstract class ArmValue<T> : ArmValue
    {
        protected ArmValue(T value, ArmType armType)
            : base(value, armType)
        {
            Value = value;
        }

        public T Value { get; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is ArmValue<T> armVal))
            {
                return false;
            }

            return Equals(Value, armVal.Value);
        }

        public override int GetHashCode()
        {
            if (Value == null)
            {
                return 0;
            }

            return Value.GetHashCode();
        }
    }
}

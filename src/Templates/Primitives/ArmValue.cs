using Newtonsoft.Json.Linq;

namespace PSArm.Templates.Primitives
{
    public abstract class ArmValue<T> : ArmElement
    {
        public ArmValue(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public override JToken ToJson()
        {
            return new JValue(Value);
        }

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

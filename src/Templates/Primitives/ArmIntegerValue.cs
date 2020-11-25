namespace PSArm.Templates.Primitives
{
    public sealed class ArmIntegerValue : ArmValue<long>
    {
        public ArmIntegerValue(long value) : base(value)
        {
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}

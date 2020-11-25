namespace PSArm.Templates.Primitives
{
    public sealed class ArmNullValue : ArmValue<object>
    {
        public static new ArmNullValue Value { get; } = new ArmNullValue();

        private ArmNullValue() : base(null)
        {
        }
    }
}

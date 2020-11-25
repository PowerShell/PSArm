namespace PSArm.Templates.Primitives
{
    public sealed class ArmStringValue : ArmValue<string>, IArmString
    {
        public ArmStringValue(string value) : base(value)
        {
        }
    }
}

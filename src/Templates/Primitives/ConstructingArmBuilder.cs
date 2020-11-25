namespace PSArm.Templates.Primitives
{
    public class ConstructingArmBuilder<T> : ArmBuilder<T> where T : ArmObject, new()
    {
        public ConstructingArmBuilder() : base(new T())
        {
        }
    }
}

using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    public class ArmVariable
    {
        public ArmVariable(IArmString name, IArmString value)
        {
            Name = name;
        }

        public IArmString Name { get; }
    }
}

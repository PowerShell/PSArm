using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    public class ArmParameter : ArmObject
    {
        public ArmParameter(IArmString name)
        {
            Name = name;
        }

        public IArmString Name { get; }

        public IArmString Type
        {
            get => (IArmString)this[ArmTemplateKeys.Type];
            set => this[ArmTemplateKeys.Type] = (ArmElement)value;
        }

        public ArmElement DefaultValue
        {
            get => this[ArmTemplateKeys.DefaultValue];
            set => this[ArmTemplateKeys.DefaultValue] = value;
        }

        public ArmArray AllowedValues
        {
            get => (ArmArray)this[ArmTemplateKeys.AllowedValues];
            set => this[ArmTemplateKeys.AllowedValues] = value;
        }
    }
}

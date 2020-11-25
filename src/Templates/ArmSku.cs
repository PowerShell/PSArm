using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    public class ArmSku : ArmObject
    {
        public IArmString Name
        {
            get => (IArmString)this[ArmTemplateKeys.Name];
            set => this[ArmTemplateKeys.Name] = (ArmElement)value;
        }

        public IArmString Tier
        {
            get => (IArmString)this[ArmTemplateKeys.Tier];
            set => this[ArmTemplateKeys.Tier] = (ArmElement)value;
        }

        public IArmString Size
        {
            get => (IArmString)this[ArmTemplateKeys.Size];
            set => this[ArmTemplateKeys.Size] = (ArmElement)value;
        }

        public IArmString Family
        {
            get => (IArmString)this[ArmTemplateKeys.Family];
            set => this[ArmTemplateKeys.Family] = (ArmElement)value;
        }

        public IArmString Capacity
        {
            get => (IArmString)this[ArmTemplateKeys.Capacity];
            set => this[ArmTemplateKeys.Capacity] = (ArmElement)value;
        }
    }
}

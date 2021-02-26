using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;

namespace PSArm.Templates
{
    public class ArmResource : ArmObject
    {
        public IArmString ApiVersion
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.ApiVersion);
            set => this[ArmTemplateKeys.ApiVersion] = (ArmElement)value;
        }

        public IArmString Type
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Type);
            set => this[ArmTemplateKeys.Type] = (ArmElement)value;
        }

        public IArmString Name
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Name);
            set => this[ArmTemplateKeys.Name] = (ArmElement)value;
        }

        public IArmString Location
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Location);
            set => this[ArmTemplateKeys.Location] = (ArmElement)value;
        }

        public IArmString Kind
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Kind);
            set => this[ArmTemplateKeys.Kind] = (ArmElement)value;
        }

        public ArmObject Properties
        {
            get => (ArmObject)GetElementOrNull(ArmTemplateKeys.Properties);
            set => this[ArmTemplateKeys.Properties] = value;
        }

        public ArmObject<ArmResource> Resources
        {
            get => (ArmObject<ArmResource>)GetElementOrNull(ArmTemplateKeys.Resources);
            set => this[ArmTemplateKeys.Resources] = value;
        }

        public ArmSku Sku
        {
            get => (ArmSku)GetElementOrNull(ArmTemplateKeys.Sku);
            set => this[ArmTemplateKeys.Sku] = value;
        }

        public ArmArray DependsOn
        {
            get => (ArmArray)GetElementOrNull(ArmTemplateKeys.DependsOn);
            set => this[ArmTemplateKeys.DependsOn] = value;
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitResource(this);
    }
}

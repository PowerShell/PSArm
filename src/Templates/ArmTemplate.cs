using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;

namespace PSArm.Templates
{
    public class ArmTemplate : ArmObject
    {
        public IArmString Schema
        {
            get => (IArmString)this[ArmTemplateKeys.Schema];
            set => this[ArmTemplateKeys.Schema] = (ArmElement)value;
        }

        public IArmString ContentVersion
        {
            get => (IArmString)this[ArmTemplateKeys.ContentVersion];
            set => this[ArmTemplateKeys.ContentVersion] = (ArmElement)value;
        }

        public ArmObject<ArmOutput> Outputs
        {
            get => (ArmObject<ArmOutput>)this[ArmTemplateKeys.Outputs];
            set => this[ArmTemplateKeys.Outputs] = value;
        }

        public ArmObject<ArmParameter> Parameters
        {
            get => (ArmObject<ArmParameter>)this[ArmTemplateKeys.Outputs];
            set => this[ArmTemplateKeys.Parameters] = value;
        }

        public ArmObject<ArmVariable> Variables
        {
            get => (ArmObject<ArmVariable>)this[ArmTemplateKeys.Variables];
            set => this[ArmTemplateKeys.Variables] = value;
        }

        public ArmArray<ArmResource> Resources
        {
            get => (ArmArray<ArmResource>)this[ArmTemplateKeys.Resources];
            set => this[ArmTemplateKeys.Resources] = value;
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitTemplate(this);
    }
}

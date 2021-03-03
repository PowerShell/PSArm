
// Copyright (c) Microsoft Corporation.

namespace PSArm.Templates.Metadata
{
    public class PSArmTopLevelTemplateMetadata : ArmMetadata
    {
        public PSArmTopLevelTemplateMetadata()
        {
            GeneratorMetadata = new PSArmGeneratorMetadata();
        }

        public PSArmGeneratorMetadata GeneratorMetadata
        {
            get => (PSArmGeneratorMetadata)GetElementOrNull(ArmTemplateKeys.GeneratorKey);
            set => this[ArmTemplateKeys.GeneratorKey] = value;
        }
    }
}

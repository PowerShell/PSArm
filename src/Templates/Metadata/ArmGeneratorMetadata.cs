
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;

namespace PSArm.Templates.Metadata
{
    public class ArmGeneratorMetadata : ArmObject<ArmStringLiteral>
    {
        public ArmStringLiteral Name
        {
            get => GetElementOrNull(ArmTemplateKeys.Name);
            set => this[ArmTemplateKeys.Name] = value;
        }

        public ArmStringLiteral Version
        {
            get => GetElementOrNull(ArmTemplateKeys.Version);
            set => this[ArmTemplateKeys.Version] = value;
        }

        public ArmStringLiteral TemplateHash
        {
            get => GetElementOrNull(ArmTemplateKeys.TemplateHash);
            set => this[ArmTemplateKeys.TemplateHash] = value;
        }
    }
}

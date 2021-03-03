
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;

namespace PSArm.Templates.Metadata
{
    public class ArmMetadata : ArmObject
    {
        public IArmString Comments
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Comments);
            set => this[ArmTemplateKeys.Comments] = (ArmElement)value;
        }
    }
}

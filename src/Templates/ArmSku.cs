
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;

namespace PSArm.Templates
{
    public class ArmSku : ArmObject
    {
        public IArmString Name
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Name);
            set => this[ArmTemplateKeys.Name] = (ArmElement)value;
        }

        public IArmString Tier
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Tier);
            set => this[ArmTemplateKeys.Tier] = (ArmElement)value;
        }

        public IArmString Size
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Size);
            set => this[ArmTemplateKeys.Size] = (ArmElement)value;
        }

        public IArmString Family
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Family);
            set => this[ArmTemplateKeys.Family] = (ArmElement)value;
        }

        public IArmString Capacity
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Capacity);
            set => this[ArmTemplateKeys.Capacity] = (ArmElement)value;
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitSku(this);
    }
}

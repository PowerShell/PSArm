﻿using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;

namespace PSArm.Templates
{
    public class ArmOutput : ArmObject
    {
        public ArmOutput(IArmString name)
        {
            Name = name;
        }

        public IArmString Name { get; }

        public IArmString Type
        {
            get => (IArmString)this[ArmTemplateKeys.Type];
            set => this[ArmTemplateKeys.Type] = (ArmElement)value;
        }

        public IArmString Value
        {
            get => (IArmString)this[ArmTemplateKeys.Value];
            set => this[ArmTemplateKeys.Value] = (ArmElement)value;
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitOutput(this);
    }
}
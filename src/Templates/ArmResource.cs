﻿using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;

namespace PSArm.Templates
{
    public class ArmResource : ArmObject
    {
        public IArmString ApiVersion
        {
            get => (IArmString)this[ArmTemplateKeys.ApiVersion];
            set => this[ArmTemplateKeys.ApiVersion] = (ArmElement)value;
        }

        public IArmString Type
        {
            get => (IArmString)this[ArmTemplateKeys.Type];
            set => this[ArmTemplateKeys.Type] = (ArmElement)value;
        }

        public IArmString Name
        {
            get => (IArmString)this[ArmTemplateKeys.Name];
            set => this[ArmTemplateKeys.Name] = (ArmElement)value;
        }

        public IArmString Location
        {
            get => (IArmString)this[ArmTemplateKeys.Location];
            set => this[ArmTemplateKeys.Location] = (ArmElement)value;
        }

        public IArmString Kind
        {
            get => (IArmString)this[ArmTemplateKeys.Kind];
            set => this[ArmTemplateKeys.Kind] = (ArmElement)value;
        }

        public ArmObject<ArmObject> Properties
        {
            get => (ArmObject<ArmObject>)this[ArmTemplateKeys.Properties];
            set => this[ArmTemplateKeys.Properties] = value;
        }

        public ArmObject<ArmResource> Resources
        {
            get => (ArmObject<ArmResource>)this[ArmTemplateKeys.Resources];
            set => this[ArmTemplateKeys.Resources] = value;
        }

        public ArmSku Sku
        {
            get => (ArmSku)this[ArmTemplateKeys.Sku];
            set => this[ArmTemplateKeys.Sku] = value;
        }

        public ArmArray DependsOn
        {
            get => (ArmArray)this[ArmTemplateKeys.DependsOn];
            set => this[ArmTemplateKeys.DependsOn] = value;
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitResource(this);
    }
}
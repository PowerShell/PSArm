
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

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

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitResource(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new ArmResource(), parameters);
    }
}

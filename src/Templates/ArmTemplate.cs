
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Metadata;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Templates
{
    public class ArmTemplate : ArmObject
    {
        private static readonly ArmStringLiteral s_defaultSchema = new ArmStringLiteral("https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#");

        private static readonly ArmStringLiteral s_defaultContentVersion = new ArmStringLiteral("1.0.0.0");

        public ArmTemplate(string templateName) : this()
        {
            TemplateName = templateName;
        }

        private protected ArmTemplate()
        {
            Schema = s_defaultSchema;
            ContentVersion = s_defaultContentVersion;
        }

        public string TemplateName { get; }

        public IArmString Schema
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Schema);
            set => this[ArmTemplateKeys.Schema] = (ArmElement)value;
        }

        public IArmString ContentVersion
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.ContentVersion);
            set => this[ArmTemplateKeys.ContentVersion] = (ArmElement)value;
        }

        public ArmMetadata Metadata
        {
            get => (ArmMetadata)GetElementOrNull(ArmTemplateKeys.Metadata);
            set => this[ArmTemplateKeys.Metadata] = value;
        }

        public ArmObject<ArmOutput> Outputs
        {
            get => (ArmObject<ArmOutput>)GetElementOrNull(ArmTemplateKeys.Outputs);
            set => this[ArmTemplateKeys.Outputs] = value;
        }

        public ArmObject<ArmParameter> Parameters
        {
            get => (ArmObject<ArmParameter>)GetElementOrNull(ArmTemplateKeys.Parameters);
            set => this[ArmTemplateKeys.Parameters] = value;
        }

        public ArmObject<ArmVariable> Variables
        {
            get => (ArmObject<ArmVariable>)GetElementOrNull(ArmTemplateKeys.Variables);
            set => this[ArmTemplateKeys.Variables] = value;
        }

        public ArmArray<ArmResource> Resources
        {
            get => (ArmArray<ArmResource>)GetElementOrNull(ArmTemplateKeys.Resources);
            set => this[ArmTemplateKeys.Resources] = value;
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitTemplate(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
        {
            var template = (ArmTemplate)InstantiateIntoCopy(new ArmTemplate(TemplateName), parameters);
            template.Remove(ArmTemplateKeys.Parameters);
            return template;
        }
    }
}

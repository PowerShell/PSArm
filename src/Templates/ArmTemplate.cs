
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Metadata;
using PSArm.Templates.Operations;
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

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitTemplate(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
        {
            IReadOnlyDictionary<IArmString, ArmElement> localParameters = GetLocalParameters(parameters);

            if (localParameters is null)
            {
                return this;
            }

            var template = (ArmTemplate)InstantiateIntoCopy(new ArmTemplate(TemplateName), localParameters);

            // Sift through any parameters declared here and remove any we instantiated
            if (template.Parameters is not null)
            {
                ArmObject<ArmParameter> templateParameters = template.Parameters;
                foreach (IArmString localParameter in localParameters.Keys)
                {
                    templateParameters.Remove(localParameter);
                }

                // If we instantiated all parameters, remove the parameter block
                if (templateParameters.Count == 0)
                {
                    template.Remove(ArmTemplateKeys.Parameters);
                }
            }

            return template;
        }

        private Dictionary<IArmString, ArmElement> GetLocalParameters(IReadOnlyDictionary<IArmString, ArmElement> globalParameters)
        {
            Dictionary<IArmString, ArmElement> parameters = null;

            if (globalParameters is not null)
            {
                parameters = new Dictionary<IArmString, ArmElement>(globalParameters.Count);

                foreach (KeyValuePair<IArmString, ArmElement> globalParameter in globalParameters)
                {
                    parameters[globalParameter.Key] = globalParameter.Value;
                }
            }

            if (Parameters is not null)
            {
                if (parameters is null)
                {
                    parameters = new Dictionary<IArmString, ArmElement>(Parameters.Count);
                }

                foreach (KeyValuePair<IArmString, ArmParameter> localParameter in (IReadOnlyDictionary<IArmString, ArmParameter>)Parameters)
                {
                    // If the parameter value is an ARM operation of some form,
                    // we must leave it as a parameter so that it's evaluated properly
                    if (localParameter.Value.DefaultValue is not null
                        && localParameter.Value.DefaultValue is not ArmOperation)
                    {
                        parameters[localParameter.Key] = localParameter.Value.DefaultValue;
                    }
                }
            }

            return parameters;
        }
    }
}

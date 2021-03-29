
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Internal;
using PSArm.Completion;
using PSArm.Internal;
using PSArm.Schema;
using PSArm.Templates;
using PSArm.Templates.Builders;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
    [OutputType(typeof(ArmEntry))]
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Resource")]
    public class NewPSArmResourceCommand : PSArmKeywordCommand, IDynamicParameters
    {
        internal const string KeywordName = "Resource";

        private static readonly HashSet<string> s_builtinParameters = new HashSet<string>(new[]
        {
            "Name",
            "Type",
            "ApiVersion",
            "DependsOn",
            "Sku",
        }, StringComparer.OrdinalIgnoreCase);

        private ResourceSchema _resourceSchema;

        private RuntimeDefinedParameterDictionary _dynamicParameters;

        [Parameter(Position = 0, Mandatory = true)]
        public IArmString Name { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter(Mandatory = true)]
        public IArmString ApiVersion { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter(Mandatory = true)]
        public IArmString Namespace { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter(Mandatory = true)]
        public IArmString Type { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            if (!TryGetResourceSchema(out ResourceSchema resourceSchema))
            {
                var resourceId = $"{Namespace}/{Type}@{ApiVersion}";
                var exception = new KeyNotFoundException($"Unable to find resource '{resourceId}'");
                this.ThrowTerminatingError(
                    exception,
                    "ResourceNotFound",
                    ErrorCategory.ObjectNotFound,
                    resourceId);
                return;
            }

            Dictionary<string, ScriptBlock> keywordDefinitions = GetDslKeywordDefinitions(resourceSchema);

            var armResource = new ConstructingArmBuilder<ArmResource>();

            armResource.AddSingleElement(ArmTemplateKeys.Name, (ArmElement)Name);
            armResource.AddSingleElement(ArmTemplateKeys.ApiVersion, (ArmElement)ApiVersion);
            armResource.AddSingleElement(ArmTemplateKeys.Type, ComposeResourceTypeElement());

            foreach (KeyValuePair<string, RuntimeDefinedParameter> dynamicParameter in _dynamicParameters)
            {
                if (resourceSchema.Discriminator is not null
                    && dynamicParameter.Key.Equals(resourceSchema.Discriminator, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                armResource.AddSingleElement(
                    new ArmStringLiteral(dynamicParameter.Key),
                    (ArmElement)dynamicParameter.Value.Value);
            }

            foreach (PSObject output in Body.InvokeWithContext(keywordDefinitions, variablesToDefine: null))
            {
                if (output.BaseObject is not ArmEntry armEntry)
                {
                    continue;
                }

                armResource.AddEntry(armEntry);
            }

            WriteObject(new ArmEntry(ArmTemplateKeys.Resources, armResource.Build(), isArrayElement: true));
        }

        public object GetDynamicParameters()
        {
            if (!TryGetResourceSchema(out ResourceSchema resourceSchema))
            {
                return null;
            }

            _dynamicParameters = new RuntimeDefinedParameterDictionary();

            foreach (string defaultParameterName in resourceSchema.SupportedResourceProperties)
            {
                if (s_builtinParameters.Contains(defaultParameterName))
                {
                    continue;
                }

                var parameter = new RuntimeDefinedParameter
                {
                    Name = defaultParameterName,
                    ParameterType = typeof(IArmString),
                };

                parameter.Attributes.Add(new ParameterAttribute());

                _dynamicParameters[defaultParameterName] = parameter;
            }

            if (resourceSchema.Discriminator is not null)
            {
                var discriminatorParameter = new RuntimeDefinedParameter
                {
                    Name = resourceSchema.Discriminator,
                    ParameterType = typeof(string),
                };

                discriminatorParameter.Attributes.Add(
                    new ParameterAttribute
                    {
                        Mandatory = true,
                    });

                discriminatorParameter.Attributes.Add(
                    new ValidateSetAttribute(
                        resourceSchema.AllowedDiscriminatorValues));

                _dynamicParameters[resourceSchema.Discriminator] = discriminatorParameter;
            }

            return _dynamicParameters;
        }

        private ArmElement ComposeResourceTypeElement()
        {
            string @namespace = Namespace.CoerceToString();
            string type = Type.CoerceToString();

            return new ArmStringLiteral($"{@namespace}/{type}");
        }

        private bool TryGetResourceSchema(out ResourceSchema resourceSchema)
        {
            if (_resourceSchema is not null)
            {
                resourceSchema = _resourceSchema;
                return true;
            }

            if (ResourceIndex.SharedInstance.TryGetResourceSchema(
                    Namespace?.CoerceToString(),
                    Type?.CoerceToString(),
                    ApiVersion?.CoerceToString(),
                    out resourceSchema))
            {
                _resourceSchema = resourceSchema;
                return true;
            }

            return false;
        }

        private Dictionary<string, ScriptBlock> GetDslKeywordDefinitions(ResourceSchema resourceSchema)
        {
            if (resourceSchema.Discriminator is null)
            {
                return resourceSchema.KeywordDefinitions;
            }

            if (!_dynamicParameters.TryGetValue(resourceSchema.Discriminator, out RuntimeDefinedParameter discriminatorParameter))
            {
                // This should be impossible since the parameter is mandatory, but nevertheless...
                var exception = new ArgumentException($"The '{resourceSchema.Discriminator}' parameter must be provided");
                this.ThrowTerminatingError(exception, "DiscriminatorParameterMissing", ErrorCategory.InvalidArgument, target: this);
                return null;
            }

            string discriminatorValue;
            try
            {
                discriminatorValue = ((IArmString)discriminatorParameter.Value).CoerceToString();
            }
            catch (Exception e)
            {
                this.ThrowTerminatingError(e, "InvalidDiscriminatorType", ErrorCategory.InvalidArgument, target: discriminatorParameter);
                return null;
            }

            if (!resourceSchema.DiscriminatedKeywords.TryGetValue(discriminatorValue, out Dictionary<string, ScriptBlock> discriminatedKeywords))
            {
                // This shouldn't be possible due to the ValidateSet attribute, but we handle it anyway
                this.ThrowTerminatingError(
                    new KeyNotFoundException($"'{discriminatorValue}' is not a valid value for parameter '{resourceSchema.Discriminator}'"),
                    "InvalidDiscriminatorValue",
                    ErrorCategory.InvalidArgument,
                    target: discriminatorValue);
                return null;
            }

            var keywords = new Dictionary<string, ScriptBlock>(resourceSchema.KeywordDefinitions, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, ScriptBlock> keywordDefinition in discriminatedKeywords)
            {
                keywords[keywordDefinition.Key] = keywordDefinition.Value;
            }

            return keywords;
        }
    }
}


// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Az;
using Azure.Bicep.Types.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PSArm.Schema
{
    public class ResourceSchema
    {
        public static IReadOnlyList<string> DefaultTopLevelProperties { get; } = new[]
        {
            "id",
            "name",
            "type",
            "apiVersion",
            "dependsOn",
            "tags",
            "condition",
            "location",
            "sku",
            "kind",
            "plan",
            "copy"
        };

        private static readonly HashSet<string> s_defaultTopLevelProperties = new HashSet<string>(DefaultTopLevelProperties, StringComparer.OrdinalIgnoreCase);

        private readonly ITypeLoader _typeLoader;

        private readonly TypeLocation _resourceTypeLocation;

        private readonly Lazy<ResourceType> _typeLazy;

        private readonly Lazy<ResourcePropertyProfile> _propertiesLazy;

        private readonly Lazy<ResourceDslDefinition> _dslDefinitionsLazy;

        public ResourceSchema(
            ITypeLoader typeLoader,
            TypeLocation resourceTypeLocation,
            string providerNamespace,
            string providerName,
            string apiVersion)
        {
            _typeLoader = typeLoader;
            _resourceTypeLocation = resourceTypeLocation;
            _typeLazy = new Lazy<ResourceType>(LoadResourceType);
            _propertiesLazy = new Lazy<ResourcePropertyProfile>(CreatePropertyProfile);
            _dslDefinitionsLazy = new Lazy<ResourceDslDefinition>(CreateResourceDefinition);
            Name = providerName;
            Namespace = providerNamespace;
            ApiVersion = apiVersion;
        }

        public string Name { get; }

        public string Namespace { get; }

        public string ApiVersion { get; }

        public ResourceType BicepType => _typeLazy.Value;

        public IReadOnlyDictionary<string, TypeBase> Properties => _propertiesLazy.Value.PropertyTable;

        public HashSet<string> SupportedResourceProperties => _propertiesLazy.Value.TopLevelDefaultProperties;

        public string Discriminator => _propertiesLazy.Value.Discriminator;

        public IReadOnlyDictionary<string, ITypeReference> DiscriminatedSubtypes => _propertiesLazy.Value.DiscriminatedSubtypes;

        public string[] AllowedDiscriminatorValues => _propertiesLazy.Value.AllowedDiscriminatorValues;

        public Dictionary<string, ScriptBlock> KeywordDefinitions => _dslDefinitionsLazy.Value.ResourceKeywordDefinitions;

        public IReadOnlyDictionary<string, Dictionary<string, ScriptBlock>> DiscriminatedKeywords => _dslDefinitionsLazy.Value.DiscriminatedKeywordDefinitions;

        private ResourceType LoadResourceType()
        {
            return _typeLoader.LoadResourceType(_resourceTypeLocation);
        }

        private ResourcePropertyProfile CreatePropertyProfile()
        {
            switch (BicepType.Body.Type)
            {
                case ObjectType objectBody:
                    return AssemblePropertyProfile(
                        objectBody.Properties,
                        additionalProperties: objectBody.AdditionalProperties?.Type);

                case DiscriminatedObjectType discriminatedBody:
                    return AssemblePropertyProfile(
                        discriminatedBody.BaseProperties,
                        discriminator: discriminatedBody.Discriminator,
                        discriminatedSubtypes: discriminatedBody.Elements);

                default:
                    throw new InvalidOperationException($"No body generated for property of type '{BicepType.Body.Type.GetType()}'");
            }
        }

        private ResourcePropertyProfile AssemblePropertyProfile(
            IDictionary<string, ObjectProperty> baseProperties,
            TypeBase additionalProperties = null,
            string discriminator = null,
            IDictionary<string, ITypeReference> discriminatedSubtypes = null)
        {
            var table = new Dictionary<string, TypeBase>();
            var defaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, ObjectProperty> propertyEntry in baseProperties)
            {
                if (s_defaultTopLevelProperties.Contains(propertyEntry.Key))
                {
                    defaults.Add(propertyEntry.Key);
                    continue;
                }

                table[propertyEntry.Key] = propertyEntry.Value.Type.Type;
            }

            if (TryGetObjectSchema(additionalProperties, out ObjectType additionalPropertiesSchema))
            {
                foreach (KeyValuePair<string, ObjectProperty> additionalPropertyEntry in additionalPropertiesSchema.Properties)
                {
                    if (s_defaultTopLevelProperties.Contains(additionalPropertyEntry.Key))
                    {
                        defaults.Add(additionalPropertyEntry.Key);
                        continue;
                    }

                    table[additionalPropertyEntry.Key] = additionalPropertyEntry.Value.Type.Type;
                }
            }

            return discriminator is null
                ? new ResourcePropertyProfile(table, defaults)
                : new ResourcePropertyProfile(table, defaults, discriminator, discriminatedSubtypes);
        }

        private bool TryGetObjectSchema(TypeBase typeBase, out ObjectType objectType)
        {
            if (typeBase is null)
            {
                objectType = null;
                return false;
            }

            if (typeBase is ObjectType obj)
            {
                objectType = obj;
                return true;
            }

            throw new ArgumentException($"Expected ObjectType schema but instead got schema of type '{typeBase.GetType()}'");
        }

        private ResourceDslDefinition CreateResourceDefinition()
        {
            return new PSArmDslFactory()
                .CreateResourceDslDefinition(Properties, DiscriminatedSubtypes);
        }

        private class ResourcePropertyProfile
        {
            public ResourcePropertyProfile(
                IReadOnlyDictionary<string, TypeBase> propertyTable,
                HashSet<string> topLevelDefaultProperties)
            {
                PropertyTable = propertyTable;
                TopLevelDefaultProperties = topLevelDefaultProperties;
            }

            public ResourcePropertyProfile(
                IReadOnlyDictionary<string, TypeBase> propertyTable,
                HashSet<string> topLevelDefaultProperties,
                string discriminator,
                IDictionary<string, ITypeReference> discriminatedSubtypes)
                : this(propertyTable, topLevelDefaultProperties)
            {
                Discriminator = discriminator;
                DiscriminatedSubtypes = (IReadOnlyDictionary<string, ITypeReference>)discriminatedSubtypes;
                AllowedDiscriminatorValues = discriminatedSubtypes.Keys.ToArray();
            }

            public IReadOnlyDictionary<string, TypeBase> PropertyTable { get; }

            public HashSet<string> TopLevelDefaultProperties { get; }

            public string Discriminator { get; }

            public IReadOnlyDictionary<string, ITypeReference> DiscriminatedSubtypes { get; }

            public string[] AllowedDiscriminatorValues { get; }
        }
    }
}

using Azure.Bicep.Types.Az;
using Azure.Bicep.Types.Concrete;
using System;
using System.Collections.Generic;

namespace PSArm.Schema
{
    public class ResourceSchema
    {
        private static readonly HashSet<string> s_defaultTopLevelProperties = new HashSet<string>(new[]
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
        }, StringComparer.OrdinalIgnoreCase);

        private readonly ITypeLoader _typeLoader;

        private readonly TypeLocation _resourceTypeLocation;

        private readonly Lazy<ResourceType> _typeLazy;

        private readonly Lazy<ResourcePropertyProfile> _propertiesLazy;

        public ResourceSchema(
            ITypeLoader typeLoader,
            TypeLocation resourceTypeLocation,
            string providerNamespace,
            string providerName,
            string apiVersion)
        {
            _typeLoader = typeLoader;
            _resourceTypeLocation = resourceTypeLocation;
            _typeLazy = new Lazy<ResourceType>(this.LoadResourceType);
            _propertiesLazy = new Lazy<ResourcePropertyProfile>(CreatePropertyProfile);
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

        private ResourceType LoadResourceType()
        {
            return _typeLoader.LoadResourceType(_resourceTypeLocation);
        }

        private ResourcePropertyProfile CreatePropertyProfile()
        {
            switch (BicepType.Body.Type)
            {
                case ObjectType objectBody:
                    var table = new Dictionary<string, TypeBase>();
                    var defaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (KeyValuePair<string, ObjectProperty> propertyEntry in objectBody.Properties)
                    {
                        if (s_defaultTopLevelProperties.Contains(propertyEntry.Key))
                        {
                            defaults.Add(propertyEntry.Key);
                            continue;
                        }

                        table[propertyEntry.Key] = propertyEntry.Value.Type.Type;
                    }
                    return new ResourcePropertyProfile(table, defaults);

                default:
                    Console.WriteLine($"No body generated for property of type '{BicepType.Body.Type.GetType()}'");
                    return null;
            }
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

            public IReadOnlyDictionary<string, TypeBase> PropertyTable { get; }

            public HashSet<string> TopLevelDefaultProperties { get; }
        }
    }
}

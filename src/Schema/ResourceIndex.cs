using Azure.Bicep.Types.Az;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PSArm.Schema
{
    public class ResourceIndex
    {
        internal static ResourceIndex SharedInstance { get; } = new ResourceIndex();

        private readonly ITypeLoader _typeLoader;

        private readonly Lazy<ResourceIndexResult> _resourceSchemasLazy;

        private readonly Lazy<IReadOnlyDictionary<string, TypeLocation>> _typeLocationTableLazy;

        private IQueryable<ResourceSchema> ResourceSchemas => _resourceSchemasLazy.Value.ResourcesQueryable;

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResourceSchema>>> ResourceSchemaTable => _resourceSchemasLazy.Value.ResourcesDictionary;

        public ResourceIndex()
            : this(new TypeLoader())
        {
        }

        public ResourceIndex(ITypeLoader typeLoader)
        {
            _typeLoader = typeLoader;
            _typeLocationTableLazy = new Lazy<IReadOnlyDictionary<string, TypeLocation>>(GetAllAvailableTypeLocations);
            _resourceSchemasLazy = new Lazy<ResourceIndexResult>(LoadResourceSchemas);
        }

        private IReadOnlyDictionary<string, TypeLocation> AvailableTypeLocationList => _typeLocationTableLazy.Value;

        public IQueryable<ResourceSchema> GetResourceSchemas() => ResourceSchemas;

        public bool TryGetResourceSchema(
            string providerNamespace,
            string providerName,
            string apiVersion,
            out ResourceSchema resourceSchema)
        {
            resourceSchema = null;
            return ResourceSchemaTable.TryGetValue(providerNamespace, out IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResourceSchema>> namespaceTable)
                && namespaceTable.TryGetValue(providerName, out IReadOnlyDictionary<string, ResourceSchema> providerTable)
                && providerTable.TryGetValue(apiVersion, out resourceSchema);
        }

        private ResourceIndexResult LoadResourceSchemas()
        {
            var providerList = new List<ResourceSchema>(AvailableTypeLocationList.Count);
            var resourceNamespaces = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResourceSchema>>>();
            foreach (KeyValuePair<string, TypeLocation> resourceType in AvailableTypeLocationList)
            {
                (string providerNamespace, string providerName, string apiVersion) = GetResourceNameComponents(resourceType.Key);

                var resource = new ResourceSchema(
                    _typeLoader,
                    resourceType.Value,
                    providerNamespace,
                    providerName,
                    apiVersion);

                providerList.Add(resource);

                if (!resourceNamespaces.TryGetValue(providerNamespace, out IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResourceSchema>> resourceNamespace))
                {
                    resourceNamespace = new Dictionary<string, IReadOnlyDictionary<string, ResourceSchema>>();
                    resourceNamespaces[providerNamespace] = resourceNamespace;
                }

                if (!resourceNamespace.TryGetValue(providerName, out IReadOnlyDictionary<string, ResourceSchema> resourceApiSet))
                {
                    resourceApiSet = new Dictionary<string, ResourceSchema>();
                    ((Dictionary<string, IReadOnlyDictionary<string, ResourceSchema>>)resourceNamespace)[providerName] = resourceApiSet;
                }

                if (!resourceApiSet.TryGetValue(apiVersion, out ResourceSchema _))
                {
                    ((Dictionary<string, ResourceSchema>)resourceApiSet)[apiVersion] = resource;
                }
            }

            return new ResourceIndexResult
            {
                ResourcesQueryable = providerList.AsQueryable(),
                ResourcesDictionary = resourceNamespaces,
            };
        }

        private IReadOnlyDictionary<string, TypeLocation> GetAllAvailableTypeLocations()
        {
            return _typeLoader.ListAllAvailableTypes();
        }

        private static (string, string, string) GetResourceNameComponents(string resourceIndexKey)
        {
            int providerNameStartIndex = resourceIndexKey.IndexOf('/') + 1;
            int apiVersionStartIndex = resourceIndexKey.IndexOf('@', providerNameStartIndex) + 1;

            string providerNamespace = resourceIndexKey.Substring(0, providerNameStartIndex - 1);
            string providerName = resourceIndexKey.Substring(providerNameStartIndex, apiVersionStartIndex - providerNameStartIndex - 1);
            string providerApiVersion = resourceIndexKey.Substring(apiVersionStartIndex);
            return (providerNamespace, providerName, providerApiVersion);
        }

        private struct ResourceIndexResult
        {
            public IQueryable<ResourceSchema> ResourcesQueryable;
            public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResourceSchema>>> ResourcesDictionary;
        }
    }
}

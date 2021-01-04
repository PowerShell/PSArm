using Azure.Bicep.Types.Az;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PSArm.Schema
{
    public class ResourceIndex
    {
        private readonly ITypeLoader _typeLoader;

        private readonly Lazy<IQueryable<ResourceSchema>> _resourceSchemasLazy;

        private IQueryable<ResourceSchema> ResourceSchemas => _resourceSchemasLazy.Value;

        public ResourceIndex()
            : this(new TypeLoader())
        {
        }

        public ResourceIndex(ITypeLoader typeLoader)
        {
            _typeLoader = typeLoader;
            _resourceSchemasLazy = new Lazy<IQueryable<ResourceSchema>>(GetAllResourceSchemas);
        }

        public IQueryable<ResourceSchema> GetResourceSchemas() => ResourceSchemas;

        private IQueryable<ResourceSchema> GetAllResourceSchemas()
        {
            IReadOnlyDictionary<string, TypeLocation> typeLocations = _typeLoader.ListAllAvailableTypes();
            var providerList = new List<ResourceSchema>(typeLocations.Count);
            foreach (KeyValuePair<string, TypeLocation> resourceType in typeLocations)
            {
                (string providerNamespace, string providerName, string apiVersion) = GetResourceNameComponents(resourceType.Key);

                providerList.Add(new ResourceSchema(
                    _typeLoader,
                    resourceType.Value,
                    providerNamespace,
                    providerName,
                    apiVersion));
            }

            return providerList.AsQueryable();
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
    }
}

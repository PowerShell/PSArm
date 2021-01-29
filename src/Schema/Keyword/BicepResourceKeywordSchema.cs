using Azure.Bicep.Types.Concrete;
using PSArm.Schema.Keyword;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PSArm.Schema
{
    public class ResourceKeywordSchema : DslKeywordSchema
    {
        private static readonly ConcurrentDictionary<ArmResourceName, ResourceKeywordCache> s_resourceKeywordCaches;

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(object context)
        {
            var resourceName = (ArmResourceName)context;

            if (!ResourceIndex.SharedInstance.TryGetResourceSchema(
                resourceName.Namespace,
                resourceName.Type,
                resourceName.ApiVersion,
                out ResourceSchema resource))
            {
                return null;
            }

            if (resource.Discriminator == null)
            {
                return resource.Properties;
            }
        }
    }

    public abstract class ResourceKeywordCache
    {
        protected ResourceKeywordCache(ResourceSchema resource)
        {
            Resource = resource;
        }

        protected ResourceSchema Resource { get; }

        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords();
    }

    public class ObjectResourceKeywordCache : ResourceKeywordCache
    {
        private Lazy<IReadOnlyDictionary<string, DslKeywordSchema>> _keywordsLazy;

        public ObjectResourceKeywordCache(ResourceSchema resource)
            : base(resource)
        {
            _keywordsLazy = new Lazy<IReadOnlyDictionary<string, DslKeywordSchema>>(GetKeywordTableFromResource);
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords()
        {
            return _keywordsLazy.Value;
        }

        private IReadOnlyDictionary<string, DslKeywordSchema> GetKeywordTableFromResource()
        {
            var dict = new Dictionary<string, DslKeywordSchema>();
            foreach (KeyValuePair<string, TypeBase> property in Resource.Properties)
            {
                dict[property.Key] = BicepKeywordSchemaGeneration.GetKeywordSchemaForBicepType(property.Value);
            }
            return dict;
        }
    }
}

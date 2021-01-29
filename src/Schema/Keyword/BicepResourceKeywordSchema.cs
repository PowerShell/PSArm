using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using PSArm.Schema.Keyword;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PSArm.Schema
{
    internal class ResourceKeywordSchema : DslKeywordSchema
    {
        private static readonly ConcurrentDictionary<ArmResourceName, ResourceKeywordCache> s_resourceKeywordCaches;

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContext context)
        {
            if (!ResourceIndex.SharedInstance.TryGetResourceSchema(
                context.ResourceNamespace,
                context.ResourceTypeName,
                context.ResourceApiVersion,
                out ResourceSchema resource))
            {
                return null;
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

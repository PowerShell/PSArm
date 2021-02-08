
using PSArm.Completion;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class ResourceKeywordSchema : DslKeywordSchema
    {
        private static readonly ConcurrentDictionary<ArmResourceName, ResourceKeywordCache> s_resourceKeywordCaches = new ConcurrentDictionary<ArmResourceName, ResourceKeywordCache>();

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context)
        {
            if (!ResourceIndex.SharedInstance.TryGetResourceSchema(
                context.ResourceNamespace,
                context.ResourceTypeName,
                context.ResourceApiVersion,
                out ResourceSchema resource))
            {
                return null;
            }

            ResourceKeywordCache cache = s_resourceKeywordCaches.GetOrAdd(
                new ArmResourceName(context.ResourceNamespace, context.ResourceTypeName, context.ResourceApiVersion),
                (resourceName) => resource.Discriminator != null ? new DiscriminatedResourceKeywordCache(resource) : new ObjectResourceKeywordCache(resource));

            return cache.GetInnerKeywords(context);
        }
    }
}

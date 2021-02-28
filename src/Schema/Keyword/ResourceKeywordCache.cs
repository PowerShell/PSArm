
// Copyright (c) Microsoft Corporation.

using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class ResourceKeywordCache
    {
        protected ResourceKeywordCache(ResourceSchema resource)
        {
            Resource = resource;
        }

        protected ResourceSchema Resource { get; }

        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context);
    }
}

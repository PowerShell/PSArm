
// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using System;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class ObjectResourceKeywordCache : ResourceKeywordCache
    {
        private Lazy<IReadOnlyDictionary<string, DslKeywordSchema>> _keywordsLazy;

        public ObjectResourceKeywordCache(ResourceSchema resource)
            : base(resource)
        {
            _keywordsLazy = new Lazy<IReadOnlyDictionary<string, DslKeywordSchema>>(GetKeywordTableFromResource);
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context)
        {
            return _keywordsLazy.Value;
        }

        private IReadOnlyDictionary<string, DslKeywordSchema> GetKeywordTableFromResource()
        {
            var dict = new Dictionary<string, DslKeywordSchema>();
            foreach (KeyValuePair<string, TypeBase> property in Resource.Properties)
            {
                dict[property.Key] = BicepKeywordSchemaBuilder.GetKeywordSchemaForBicepType(property.Value);
            }
            return dict;
        }
    }
}

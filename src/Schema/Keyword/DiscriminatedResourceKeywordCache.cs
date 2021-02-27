
// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using PSArm.Schema.Keyword;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class DiscriminatedResourceKeywordCache : ResourceKeywordCache
    {
        private ConcurrentDictionary<string, IReadOnlyDictionary<string, DslKeywordSchema>> _discriminatedKeywordTables;

        private Lazy<Dictionary<string, DslKeywordSchema>> _commonKeywordsLazy;

        public DiscriminatedResourceKeywordCache(ResourceSchema resource)
            : base(resource)
        {
            _discriminatedKeywordTables = new ConcurrentDictionary<string, IReadOnlyDictionary<string, DslKeywordSchema>>();
            _commonKeywordsLazy = new Lazy<Dictionary<string, DslKeywordSchema>>(BuildCommonKeywordDictionary);
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context)
        {
            string discriminatorValue = context.GetDiscriminatorValue(Resource.Discriminator);

            if (discriminatorValue is null
                || !Resource.DiscriminatedSubtypes.ContainsKey(discriminatorValue))
            {
                return null;
            }

            return _discriminatedKeywordTables.GetOrAdd(discriminatorValue, BuildDiscriminatedKeywordDictionary);
        }

        private Dictionary<string, DslKeywordSchema> BuildCommonKeywordDictionary()
        {
            var dict = new Dictionary<string, DslKeywordSchema>(Resource.Properties.Count);
            foreach (KeyValuePair<string, TypeBase> property in Resource.Properties)
            {
                dict[property.Key] = BicepKeywordSchemaBuilder.GetKeywordSchemaForBicepType(property.Value);
            }
            return dict;
        }

        private IReadOnlyDictionary<string, DslKeywordSchema> BuildDiscriminatedKeywordDictionary(string discriminatorValue)
        {
            TypeBase discriminatedType = Resource.DiscriminatedSubtypes[discriminatorValue].Type;

            if (discriminatedType is not ObjectType objectType)
            {
                throw new ArgumentException($"Discriminated schema element has non-object type '{discriminatedType.GetType()}'");
            }

            var dict = new Dictionary<string, DslKeywordSchema>(_commonKeywordsLazy.Value);
            foreach (KeyValuePair<string, ObjectProperty> discriminatedProperty in objectType.Properties)
            {
                dict[discriminatedProperty.Key] = BicepKeywordSchemaBuilder.GetKeywordSchemaForBicepType(discriminatedProperty.Value.Type.Type);
            }
            return dict;
        }
    }
}

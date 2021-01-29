using Azure.Bicep.Types.Concrete;
using System;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    public class BicepObjectKeywordSchema : BicepKeywordSchema<ObjectType>
    {
        private readonly Lazy<IReadOnlyDictionary<string, DslKeywordSchema>> _innerKeywordsLazy;

        public BicepObjectKeywordSchema(ObjectType objectType)
            : base(objectType)
        {
            _innerKeywordsLazy = new Lazy<IReadOnlyDictionary<string, DslKeywordSchema>>(BuildInnerKeywordDict);
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(object context) => _innerKeywordsLazy.Value;

        private IReadOnlyDictionary<string, DslKeywordSchema> BuildInnerKeywordDict()
        {
            var dict = new Dictionary<string, DslKeywordSchema>();
            foreach (KeyValuePair<string, ObjectProperty> property in BicepType.Properties)
            {
                dict[property.Key] = BicepKeywordSchemaGeneration.GetKeywordSchemaForBicepType(property.Value.Type.Type);
            }
            return dict;
        }
    }
}

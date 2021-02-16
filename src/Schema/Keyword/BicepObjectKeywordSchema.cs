using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using PSArm.Internal;
using System;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class BicepObjectKeywordSchema : BicepKeywordSchema<ObjectType>
    {
        private readonly Lazy<IReadOnlyDictionary<string, DslKeywordSchema>> _innerKeywordsLazy;

        public BicepObjectKeywordSchema(ObjectType objectType)
            : base(objectType)
        {
            _innerKeywordsLazy = new Lazy<IReadOnlyDictionary<string, DslKeywordSchema>>(BuildInnerKeywordDict);
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context) => _innerKeywordsLazy.Value;

        public override IEnumerable<string> GetParameterNames(KeywordContextFrame context)
        {
            return BodyParameter;
        }

        public override string GetParameterType(KeywordContextFrame context, string parameterName)
        {
            if (parameterName.Is("Body"))
            {
                return "scriptblock";
            }

            return null;
        }

        public override IEnumerable<string> GetParameterValues(KeywordContextFrame context, string parameterName)
        {
            return null;
        }

        private IReadOnlyDictionary<string, DslKeywordSchema> BuildInnerKeywordDict()
        {
            var dict = new Dictionary<string, DslKeywordSchema>();
            foreach (KeyValuePair<string, ObjectProperty> property in BicepType.Properties)
            {
                dict[property.Key] = BicepKeywordSchemaBuilder.GetKeywordSchemaForBicepType(property.Value.Type.Type);
            }
            return dict;
        }
    }
}

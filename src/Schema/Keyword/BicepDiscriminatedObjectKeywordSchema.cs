using Azure.Bicep.Types.Concrete;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    public class BicepDiscriminatedObjectKeywordSchema : BicepKeywordSchema<DiscriminatedObjectType>
    {
        private readonly Lazy<Dictionary<string, DslKeywordSchema>> _commonKeywords;

        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, DslKeywordSchema>> _discriminatedInnerKeywords;

        public BicepDiscriminatedObjectKeywordSchema(DiscriminatedObjectType discriminatedObjectType)
            : base(discriminatedObjectType)
        {
            _discriminatedInnerKeywords = new ConcurrentDictionary<string, IReadOnlyDictionary<string, DslKeywordSchema>>();
            _commonKeywords = new Lazy<Dictionary<string, DslKeywordSchema>>(BuildCommonKeywordDictionary);
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(object context)
        {
            var discriminatorValue = (string)context;
            return _discriminatedInnerKeywords.GetOrAdd(discriminatorValue, BuildDiscriminatedKeywordDictionary);
        }

        private Dictionary<string, DslKeywordSchema> BuildCommonKeywordDictionary()
        {
            var dict = new Dictionary<string, DslKeywordSchema>(BicepType.BaseProperties.Count);
            foreach (KeyValuePair<string, ObjectProperty> property in BicepType.BaseProperties)
            {
                dict[property.Key] = BicepKeywordSchemaGeneration.GetKeywordSchemaForBicepType(property.Value.Type.Type);
            }
            return dict;
        }

        private IReadOnlyDictionary<string, DslKeywordSchema> BuildDiscriminatedKeywordDictionary(string discriminatorValue)
        {
            TypeBase discriminatedType = BicepType.Elements[discriminatorValue].Type;

            if (discriminatedType is not ObjectType objectType)
            {
                throw new ArgumentException($"Discriminated schema element has non-object type '{discriminatedType.GetType()}'");
            }

            var dict = new Dictionary<string, DslKeywordSchema>(_commonKeywords.Value);
            foreach (KeyValuePair<string, ObjectProperty> discriminatedProperty in objectType.Properties)
            {
                dict[discriminatedProperty.Key] = BicepKeywordSchemaGeneration.GetKeywordSchemaForBicepType(discriminatedProperty.Value.Type.Type);
            }
            return dict;
        }
    }
}

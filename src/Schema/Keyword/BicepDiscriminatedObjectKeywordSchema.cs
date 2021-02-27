
// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PSArm.Schema.Keyword
{
    internal class BicepDiscriminatedObjectKeywordSchema : BicepKeywordSchema<DiscriminatedObjectType>
    {
        private readonly Lazy<Dictionary<string, DslKeywordSchema>> _commonKeywords;

        private readonly Lazy<IReadOnlyDictionary<string, DslParameterInfo>> _parameters;

        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, DslKeywordSchema>> _discriminatedInnerKeywords;

        private IReadOnlyDictionary<string, DslParameterInfo> Parameters => _parameters.Value;

        public BicepDiscriminatedObjectKeywordSchema(DiscriminatedObjectType discriminatedObjectType)
            : base(discriminatedObjectType)
        {
            _discriminatedInnerKeywords = new ConcurrentDictionary<string, IReadOnlyDictionary<string, DslKeywordSchema>>();
            _commonKeywords = new Lazy<Dictionary<string, DslKeywordSchema>>(BuildCommonKeywordDictionary);
            _parameters = new Lazy<IReadOnlyDictionary<string, DslParameterInfo>>(BuildParameterDictionary);
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context)
        {
            string discriminatorValue = context.GetDiscriminatorValue(BicepType.Discriminator);

            if (discriminatorValue is null
                || !BicepType.Elements.ContainsKey(discriminatorValue))
            {
                return null;
            }

            return _discriminatedInnerKeywords.GetOrAdd(discriminatorValue, BuildDiscriminatedKeywordDictionary);
        }

        public override IEnumerable<string> GetParameterNames(KeywordContextFrame context)
            => Parameters.Keys;

        public override string GetParameterType(KeywordContextFrame context, string parameterName)
        {
            return Parameters.TryGetValue(parameterName, out DslParameterInfo parameterInfo)
                ? parameterInfo.Type
                : null;
        }

        public override IEnumerable<string> GetParameterValues(KeywordContextFrame context, string parameterName)
        {
            return Parameters.TryGetValue(parameterName, out DslParameterInfo parameterInfo)
                ? parameterInfo.Values
                : null;
        }

        private Dictionary<string, DslKeywordSchema> BuildCommonKeywordDictionary()
        {
            var dict = new Dictionary<string, DslKeywordSchema>(BicepType.BaseProperties.Count);
            foreach (KeyValuePair<string, ObjectProperty> property in BicepType.BaseProperties)
            {
                dict[property.Key] = BicepKeywordSchemaBuilder.GetKeywordSchemaForBicepType(property.Value.Type.Type);
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
                dict[discriminatedProperty.Key] = BicepKeywordSchemaBuilder.GetKeywordSchemaForBicepType(discriminatedProperty.Value.Type.Type);
            }
            return dict;
        }

        private IReadOnlyDictionary<string, DslParameterInfo> BuildParameterDictionary()
        {
            return new Dictionary<string, DslParameterInfo>
            {
                { "Body", new DslParameterInfo("Body", "scriptblock") },
                { BicepType.Discriminator, new DslParameterInfo(BicepType.Discriminator, "string", BicepType.Elements.Keys.ToArray()) },
            };
        }
    }
}

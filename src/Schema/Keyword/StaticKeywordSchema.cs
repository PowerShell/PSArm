using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class StaticKeywordSchema : DslKeywordSchema
    {
        private readonly IReadOnlyDictionary<string, DslKeywordSchema> _innerKeywords;

        internal StaticKeywordSchema(
            IReadOnlyDictionary<string, DslParameterInfo> parameters,
            IReadOnlyDictionary<string, DslKeywordSchema> schema)
        {
            Parameters = parameters;
            _innerKeywords = schema;
        }

        public override IReadOnlyDictionary<string, DslParameterInfo> Parameters { get; }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context) => _innerKeywords;
    }
}

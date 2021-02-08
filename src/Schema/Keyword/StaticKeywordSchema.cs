using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class StaticKeywordSchema : DslKeywordSchema
    {
        private readonly IReadOnlyDictionary<string, DslKeywordSchema> _innerKeywords;

        internal StaticKeywordSchema(
            IReadOnlyDictionary<string, DslKeywordSchema> schema)
        {
            _innerKeywords = schema;
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context) => _innerKeywords;
    }
}

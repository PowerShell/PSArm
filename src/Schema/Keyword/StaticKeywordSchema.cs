using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    public class StaticKeywordSchema : DslKeywordSchema
    {
        private readonly IReadOnlyDictionary<string, DslKeywordSchema> _innerKeywords;

        internal StaticKeywordSchema(
            IReadOnlyDictionary<string, DslKeywordSchema> schema)
        {
            _innerKeywords = schema;
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(object context) => _innerKeywords;
    }
}

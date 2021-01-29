using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal sealed class OpenKeywordSchema : DslKeywordSchema
    {
        public static OpenKeywordSchema Value { get; } = new OpenKeywordSchema();

        private OpenKeywordSchema()
        {
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContext context) => null;
    }
}

using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal sealed class OpenKeywordSchema : DslKeywordSchema
    {
        public OpenKeywordSchema(
            IReadOnlyDictionary<string, DslParameterInfo> parameters)
        {
            Parameters = parameters;
        }

        public override IReadOnlyDictionary<string, DslParameterInfo> Parameters { get; }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context) => null;
    }
}

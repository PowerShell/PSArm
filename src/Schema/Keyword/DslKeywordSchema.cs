using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class DslKeywordSchema
    {
        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContext context);
    }
}

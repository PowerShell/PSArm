using PSArm.Completion;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema
{
    internal abstract class DslKeywordSchema
    {
        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContext context);
    }
}

using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class DslKeywordSchema
    {
        public abstract IEnumerable<string> GetParameterNames(KeywordContextFrame context);

        public abstract DslParameterInfo GetParameterValueInfo(KeywordContextFrame context, string parameterName);

        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context);
    }
}

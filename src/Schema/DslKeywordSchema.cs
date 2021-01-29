using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema
{
    public abstract class DslKeywordSchema
    {
        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(object context);
    }
}

using Azure.Bicep.Types.Concrete;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    public sealed class OpenKeywordSchema : DslKeywordSchema
    {
        public static OpenKeywordSchema Value { get; } = new OpenKeywordSchema();

        private OpenKeywordSchema()
        {
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(object context) => null;
    }
}

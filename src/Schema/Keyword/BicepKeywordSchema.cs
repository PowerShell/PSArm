using Azure.Bicep.Types.Concrete;
using System.Collections;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class BicepKeywordSchema<TBicepType> : DslKeywordSchema where TBicepType : TypeBase
    {
        protected static IEnumerable<string> BodyParameter { get; } = new[] { "Body" };

        public BicepKeywordSchema(TBicepType bicepType)
        {
            BicepType = bicepType;
        }

        public override bool ShouldUseDefaultParameterCompletions => false;

        public TBicepType BicepType { get; }
    }
}

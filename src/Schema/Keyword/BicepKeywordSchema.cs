using Azure.Bicep.Types.Concrete;

namespace PSArm.Schema.Keyword
{
    internal abstract class BicepKeywordSchema<TBicepType> : DslKeywordSchema where TBicepType : TypeBase
    {
        public BicepKeywordSchema(TBicepType bicepType)
        {
            BicepType = bicepType;
        }

        public TBicepType BicepType { get; }
    }
}

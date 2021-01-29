using Azure.Bicep.Types.Concrete;

namespace PSArm.Schema.Keyword
{
    public abstract class BicepKeywordSchema<TBicepType> : DslKeywordSchema where TBicepType : TypeBase
    {
        public BicepKeywordSchema(TBicepType bicepType)
        {
            BicepType = bicepType;
        }

        public TBicepType BicepType { get; }
    }
}

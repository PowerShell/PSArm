using Azure.Bicep.Types.Concrete;
using System;

namespace PSArm.Schema.Keyword
{
    internal static class BicepKeywordSchemaGeneration
    {
        public static DslKeywordSchema GetKeywordSchemaForBicepType(TypeBase bicepType)
        {
            switch (bicepType)
            {
                case ObjectType objectType:
                    return new BicepObjectKeywordSchema(objectType);

                case DiscriminatedObjectType discriminatedObjectType:
                    return new BicepDiscriminatedObjectKeywordSchema(discriminatedObjectType);

                default:
                    return OpenKeywordSchema.Value;
            }
        }
    }
}

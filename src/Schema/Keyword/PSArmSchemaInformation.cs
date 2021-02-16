using PSArm.Commands.Template;
using PSArm.Internal;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal static class PSArmSchemaInformation
    {
        public static DslKeywordSchema PSArmSchema { get; } = new StaticKeywordSchema(
            parameters: null,
            new Dictionary<string, DslKeywordSchema>
            {
                { NewPSArmTemplateCommand.KeywordName, s_armKeywordSchema },
            });

        private readonly static DslKeywordSchema s_armKeywordSchema = new StaticKeywordSchema(
            KeywordParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmTemplateCommand)),
            new Dictionary<string, DslKeywordSchema>
            {
                { NewPSArmResourceCommand.KeywordName, ResourceKeywordSchema.Value },
                { NewPSArmOutputCommand.KeywordName, s_outputKeywordSchema },
            });

        private readonly static DslKeywordSchema s_outputKeywordSchema = new OpenKeywordSchema(
            KeywordParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmOutputCommand)));
    }
}

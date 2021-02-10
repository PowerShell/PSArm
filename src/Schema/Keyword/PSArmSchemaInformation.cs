using PSArm.Commands.Template;
using PSArm.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema.Keyword
{
    internal static class PSArmSchemaInformation
    {
        public static DslKeywordSchema PSArmSchema { get; } = new StaticKeywordSchema(
            null,
            new Dictionary<string, DslKeywordSchema>
            {
                { NewPSArmTemplateCommand.KeywordName, s_armKeywordSchema },
            });

        private static DslKeywordSchema s_armKeywordSchema = new StaticKeywordSchema(
            KeywordParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmTemplateCommand)),
            new Dictionary<string, DslKeywordSchema>
            {
                { NewPSArmResourceCommand.KeywordName, s_resourceKeywordSchema },
                { NewPSArmOutputCommand.KeywordName, s_outputKeywordSchema },
            });

        private static DslKeywordSchema s_outputKeywordSchema = OpenKeywordSchema.Value;

        private static DslKeywordSchema s_resourceKeywordSchema = new ResourceKeywordSchema();
    }
}

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
                { NewPSArmTemplateCommand.KeywordName, new StaticKeywordSchema(
                    KeywordParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmTemplateCommand)),
                    new Dictionary<string, DslKeywordSchema>
                    {
                        { NewPSArmResourceCommand.KeywordName, ResourceKeywordSchema.Value },
                        { NewPSArmOutputCommand.KeywordName, new OpenKeywordSchema(
                            KeywordParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmOutputCommand)),
                            useParametersForCompletions: false) }
                    })},
            });
    }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Template;
using PSArm.Internal;
using System;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal static class PSArmSchemaInformation
    {
        public static DslKeywordSchema PSArmSchema { get; } = new StaticKeywordSchema(
            parameters: null,
            new Dictionary<string, DslKeywordSchema>(StringComparer.OrdinalIgnoreCase)
            {
                { NewPSArmTemplateCommand.KeywordName, new StaticKeywordSchema(
                    KeywordPowerShellParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmTemplateCommand)),
                    new Dictionary<string, DslKeywordSchema>(StringComparer.OrdinalIgnoreCase)
                    {
                        { NewPSArmResourceCommand.KeywordName, ResourceKeywordSchema.Value },
                        { NewPSArmOutputCommand.KeywordName, new OpenKeywordSchema(
                            KeywordPowerShellParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmOutputCommand)),
                            useParametersForCompletions: false) }
                    })},
            });

        public static DslKeywordSchema SkuSchema { get; } = new OpenKeywordSchema(
            KeywordPowerShellParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmSkuCommand)),
            useParametersForCompletions: true);

        public static DslKeywordSchema DependsOnSchema { get; } = new OpenKeywordSchema(
            KeywordPowerShellParameterDiscovery.GetKeywordParametersFromCmdletType(typeof(NewPSArmDependsOnCommand)),
            useParametersForCompletions: true);
    }
}

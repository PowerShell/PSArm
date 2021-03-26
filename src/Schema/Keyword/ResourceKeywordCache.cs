
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Template;
using PSArm.Completion;
using System;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class ResourceKeywordCache
    {
        protected ResourceKeywordCache(ResourceSchema resource)
        {
            Resource = resource;
        }

        protected ResourceSchema Resource { get; }

        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context);

        protected Dictionary<string, DslKeywordSchema> GetBaseKeywordDictionary()
        {
            return new Dictionary<string, DslKeywordSchema>(StringComparer.OrdinalIgnoreCase)
            {
                { NewPSArmSkuCommand.KeywordName, PSArmSchemaInformation.SkuSchema },
                { NewPSArmDependsOnCommand.KeywordName, PSArmSchemaInformation.DependsOnSchema },
                { NewPSArmResourceCommand.KeywordName, ResourceKeywordSchema.Value },
            };
        }
    }
}

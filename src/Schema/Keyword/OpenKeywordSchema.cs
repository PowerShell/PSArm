﻿using Azure.Bicep.Types.Concrete;
using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal sealed class OpenKeywordSchema : KnownParametersSchema
    {
        public OpenKeywordSchema(IReadOnlyDictionary<string, DslParameterInfo> parameters)
            : base(parameters)
        {
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context) => null;
    }
}
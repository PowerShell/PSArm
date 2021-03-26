
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal sealed class OpenKeywordSchema : KnownParametersSchema
    {
        public OpenKeywordSchema(
            IReadOnlyDictionary<string, DslParameterInfo> parameters,
            bool useParametersForCompletions)
            : base(parameters, useParametersForCompletions)
        {
        }

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context) => null;
    }
}

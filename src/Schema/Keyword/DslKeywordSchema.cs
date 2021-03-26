
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class DslKeywordSchema
    {
        public abstract bool ShouldUseDefaultParameterCompletions { get; }

        public abstract IEnumerable<string> GetParameterNames(KeywordContextFrame context);

        public abstract IEnumerable<string> GetParameterValues(KeywordContextFrame context, string parameterName);

        public abstract string GetParameterType(KeywordContextFrame context, string parameterName);

        public abstract IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context);
    }
}

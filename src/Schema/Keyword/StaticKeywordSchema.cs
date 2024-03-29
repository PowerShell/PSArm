
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class StaticKeywordSchema : KnownParametersSchema
    {
        private readonly IReadOnlyDictionary<string, DslKeywordSchema> _innerKeywords;

        public StaticKeywordSchema(
            IReadOnlyDictionary<string, DslParameterInfo> parameters,
            IReadOnlyDictionary<string, DslKeywordSchema> schema)
            : base(parameters, useParametersForCompletions: false)
        {
            _innerKeywords = schema;
        }

        public override IEnumerable<string> GetParameterNames(KeywordContextFrame context)
            => Parameters.Keys;

        public override IReadOnlyDictionary<string, DslKeywordSchema> GetInnerKeywords(KeywordContextFrame context) => _innerKeywords;
    }
}

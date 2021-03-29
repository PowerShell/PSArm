
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class KnownParametersSchema : DslKeywordSchema
    {
        private readonly bool _shouldUseDefaultParameterCompletions;

        public KnownParametersSchema(
            IReadOnlyDictionary<string, DslParameterInfo> parameters,
            bool useParametersForCompletions)
        {
            _shouldUseDefaultParameterCompletions = !useParametersForCompletions;
            Parameters = parameters;
        }

        protected IReadOnlyDictionary<string, DslParameterInfo> Parameters { get; }

        public override bool ShouldUseDefaultParameterCompletions => _shouldUseDefaultParameterCompletions;

        public override IEnumerable<string> GetParameterNames(KeywordContextFrame context)
            => Parameters.Keys;

        public override string GetParameterType(KeywordContextFrame context, string parameterName)
        {
            if (!Parameters.TryGetValue(parameterName, out DslParameterInfo parameterInfo))
            {
                return null;
            }

            return parameterInfo.Type;
        }

        public override IEnumerable<string> GetParameterValues(KeywordContextFrame context, string parameterName)
        {
            if (!Parameters.TryGetValue(parameterName, out DslParameterInfo parameterInfo))
            {
                return null;
            }

            return parameterInfo.Values;
        }
    }
}

﻿using PSArm.Completion;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal abstract class KnownParametersSchema : DslKeywordSchema
    {
        public KnownParametersSchema(
            IReadOnlyDictionary<string, DslParameterInfo> parameters)
        {
            Parameters = parameters;
        }

        protected IReadOnlyDictionary<string, DslParameterInfo> Parameters { get; }

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
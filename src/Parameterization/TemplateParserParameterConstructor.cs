// Copyright (c) Microsoft Corporation.

using PSArm.Templates;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace PSArm.Parameterization
{
    internal class TemplateParserParameterConstructor<TParameter, TParameterReference>
        : TemplateParameterConstructor<TParameter, IReadOnlyDictionary<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>>, TParameter, IArmString, object>
        where TParameter : ArmElement, IArmReferenceable
        where TParameterReference : ArmReferenceExpression<TParameter>
    {
        private readonly IReadOnlyDictionary<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>> _parameterReferenceTable;

        private readonly IReadOnlyDictionary<IArmString, TParameter> _parameterTable;

        public TemplateParserParameterConstructor(
            IReadOnlyDictionary<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>> parameterReferenceTable)
        {
            _parameterReferenceTable = parameterReferenceTable;
            _parameterTable = CreateParameterTable(parameterReferenceTable.Keys);
        }

        public ArmObject<TParameter> ConstructParameters() => ConstructParameters(_parameterReferenceTable);

        protected override IReadOnlyDictionary<TParameter, IReadOnlyList<IArmString>> CollectReferences(IReadOnlyDictionary<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>> parameters)
        {
            var referenceTable = new Dictionary<TParameter, IReadOnlyList<IArmString>>();
            foreach (KeyValuePair<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>> entry in parameters)
            {
                referenceTable[entry.Key] = entry.Value.Keys.ToList();
            }
            return referenceTable;
        }

        protected override TParameter EvaluateParameter(object evaluationState, TParameter parameter)
        {
            // Go through and set the referenced value in each reference
            foreach (IReadOnlyDictionary<IArmString, List<TParameterReference>> referenceSet in _parameterReferenceTable.Values)
            {
                foreach (List<TParameterReference> references in referenceSet.Values)
                {
                    foreach (TParameterReference reference in references)
                    {
                        reference.ReferencedValue = _parameterTable[reference.ReferenceName];
                    }
                }
            }

            return parameter;
        }

        protected override IArmString GetParameterName(TParameter parameter)
        {
            return parameter.ReferenceName;
        }

        private static IReadOnlyDictionary<IArmString, TParameter> CreateParameterTable(IEnumerable<TParameter> parameters)
        {
            var dict = new Dictionary<IArmString, TParameter>();
            foreach (TParameter parameter in parameters)
            {
                dict[parameter.ReferenceName] = parameter;
            }
            return dict;
        }

        protected override object CreateEvaluationState()
        {
            return null;
        }
    }
}

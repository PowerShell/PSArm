// Copyright (c) Microsoft Corporation.

using PSArm.Templates;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace PSArm.Parameterization
{
    internal class TemplateParserParameterConstructor<TParameter, TParameterReference>
        : TemplateParameterConstructor<TParameter, IReadOnlyDictionary<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>>, TParameter, IArmString>
        where TParameter : ArmElement, IArmReferenceable
        where TParameterReference : ArmReferenceExpression<TParameter>
    {
        private readonly IReadOnlyDictionary<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>> _parameterReferenceTable;

        public TemplateParserParameterConstructor(
            IReadOnlyDictionary<TParameter, IReadOnlyDictionary<IArmString, List<TParameterReference>>> parameterReferenceTable)
        {
            _parameterReferenceTable = parameterReferenceTable;
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

        protected override TParameter EvaluateParameter(TParameter parameter)
        {
            // Go through and set the referenced value in each reference
            foreach (IReadOnlyDictionary<IArmString, List<TParameterReference>> referenceSet in _parameterReferenceTable.Values)
            {
                foreach (TParameterReference reference in referenceSet[parameter.ReferenceName])
                {
                    reference.ReferencedValue = parameter;
                }
            }

            return parameter;
        }

        protected override IArmString GetParameterName(TParameter parameter)
        {
            return parameter.ReferenceName;
        }
    }
}

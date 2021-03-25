// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Templates;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System.Collections.Generic;

namespace PSArm.Parameterization
{
    internal class TemplateReferenceCollector
    {
        private readonly ReferenceCollectingArmVisitor _referenceCollectingVisitor;

        public TemplateReferenceCollector()
        {
            _referenceCollectingVisitor = new ReferenceCollectingArmVisitor();
        }

        public ReferenceCollectionResult CollectReferences(IEnumerable<ArmVariable> armVariables, IEnumerable<ArmParameter> armParameters)
        {
            var variables = armVariables is not null ? new Dictionary<ArmVariable, IReadOnlyDictionary<IArmString, List<ArmVariableReferenceExpression>>>() : null;
            var parameters = armParameters is not null ? new Dictionary<ArmParameter, IReadOnlyDictionary<IArmString, List<ArmParameterReferenceExpression>>>() : null;

            if (armParameters is not null)
            {
                foreach (ArmParameter parameter in armParameters)
                {
                    _referenceCollectingVisitor.Reset();
                    parameter.DefaultValue?.RunVisit(_referenceCollectingVisitor);
                    parameters[parameter] = _referenceCollectingVisitor.Parameters.ShallowClone();
                }
            }

            if (armVariables is not null)
            {
                foreach (ArmVariable variable in armVariables)
                {
                    _referenceCollectingVisitor.Reset();
                    variable.Value.RunVisit(_referenceCollectingVisitor);
                    variables[variable] = _referenceCollectingVisitor.Variables.ShallowClone();
                }
            }

            return new ReferenceCollectionResult(variables, parameters);
        }

        public readonly struct ReferenceCollectionResult
        {
            public ReferenceCollectionResult(
                IReadOnlyDictionary<ArmVariable, IReadOnlyDictionary<IArmString, List<ArmVariableReferenceExpression>>> variables,
                IReadOnlyDictionary<ArmParameter, IReadOnlyDictionary<IArmString, List<ArmParameterReferenceExpression>>> parameters)
            {
                Variables = variables;
                Parameters = parameters;
            }

            public IReadOnlyDictionary<ArmVariable, IReadOnlyDictionary<IArmString, List<ArmVariableReferenceExpression>>> Variables { get; }
            public IReadOnlyDictionary<ArmParameter, IReadOnlyDictionary<IArmString, List<ArmParameterReferenceExpression>>> Parameters { get; }
        }
    }
}

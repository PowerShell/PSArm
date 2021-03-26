
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Parameterization
{
    internal class ReferenceCollectingArmVisitor : ArmTravsersingVisitor
    {
        private readonly Dictionary<IArmString, List<ArmVariableReferenceExpression>> _variableReferences;

        private readonly Dictionary<IArmString, List<ArmParameterReferenceExpression>> _parameterReferences;

        public ReferenceCollectingArmVisitor()
        {
            _variableReferences = new Dictionary<IArmString, List<ArmVariableReferenceExpression>>();
            _parameterReferences = new Dictionary<IArmString, List<ArmParameterReferenceExpression>>();
        }

        public IReadOnlyDictionary<IArmString, List<ArmVariableReferenceExpression>> Variables => _variableReferences;

        public IReadOnlyDictionary<IArmString, List<ArmParameterReferenceExpression>> Parameters => _parameterReferences;

        public void Reset()
        {
            _variableReferences.Clear();
            _parameterReferences.Clear();
        }

        public override VisitAction VisitVariableReference(ArmVariableReferenceExpression variableReference)
        {
            if (_variableReferences.TryGetValue(variableReference.ReferenceName, out List<ArmVariableReferenceExpression> references))
            {
                references.Add(variableReference);
                return VisitAction.Continue;
            }

            references = new List<ArmVariableReferenceExpression> { variableReference };
            _variableReferences[variableReference.ReferenceName] = references;
            return VisitAction.Continue;
        }

        public override VisitAction VisitParameterReference(ArmParameterReferenceExpression parameterReference)
        {
            if (_parameterReferences.TryGetValue(parameterReference.ReferenceName, out List<ArmParameterReferenceExpression> references))
            {
                references.Add(parameterReference);
                return VisitAction.Continue;
            }

            references = new List<ArmParameterReferenceExpression> { parameterReference };
            _parameterReferences[parameterReference.ReferenceName] = references;
            return VisitAction.Continue;
        }
    }
}

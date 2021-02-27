
// Copyright (c) Microsoft Corporation.

using Newtonsoft.Json.Linq;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;

namespace PSArm.Templates
{
    public class ArmVariable : ArmElement, IArmReferenceable<ArmVariableReferenceExpression>
    {
        public static explicit operator ArmVariableReferenceExpression(ArmVariable variable) => variable.GetReference();

        public ArmVariable(IArmString name, ArmElement value)
        {
            Name = name;
            Value = value;
        }

        public IArmString Name { get; }

        public ArmElement Value { get; }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitVariableDeclaration(this);

        public ArmVariableReferenceExpression GetReference() => new ArmVariableReferenceExpression(this);

        IArmString IArmReferenceable.ReferenceName => Name;

    }
}

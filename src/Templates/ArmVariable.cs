
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Templates
{
    public class ArmVariable : ArmElement, IArmReferenceable<ArmVariableReferenceExpression>
    {
        public static explicit operator ArmVariableReferenceExpression(ArmVariable variable) => variable.GetReference();

        public static explicit operator ArmExpression(ArmVariable variable) => (ArmVariableReferenceExpression)variable;

        public ArmVariable(IArmString name, ArmElement value)
        {
            Name = name;
            Value = value;
        }

        public IArmString Name { get; }

        public ArmElement Value { get; }

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitVariableDeclaration(this);

        public ArmVariableReferenceExpression GetReference() => new ArmVariableReferenceExpression(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => this;

        public override string ToString()
        {
            return GetReference().ToString();
        }

        IArmString IArmReferenceable.ReferenceName => Name;
    }
}

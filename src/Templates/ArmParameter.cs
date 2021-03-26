
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using PSArm.Types;
using System;
using System.Collections.Generic;

namespace PSArm.Templates
{
    public class ArmParameter : ArmObject, IArmReferenceable<ArmParameterReferenceExpression>
    {
        public static explicit operator ArmParameterReferenceExpression(ArmParameter parameter) => parameter.GetReference();

        public static explicit operator ArmExpression(ArmParameter parameter) => (ArmParameterReferenceExpression)parameter;

        public ArmParameter(IArmString name, IArmString type)
        {
            Name = name;
            Type = type;
        }

        public IArmString Name { get; }

        public IArmString Type
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Type);
            private set => this[ArmTemplateKeys.Type] = (ArmElement)value;
        }

        public ArmElement DefaultValue
        {
            get => GetElementOrNull(ArmTemplateKeys.DefaultValue);
            set => this[ArmTemplateKeys.DefaultValue] = value;
        }

        public ArmArray AllowedValues
        {
            get => (ArmArray)GetElementOrNull(ArmTemplateKeys.AllowedValues);
            set => this[ArmTemplateKeys.AllowedValues] = value;
        }

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitParameterDeclaration(this);

        public ArmParameterReferenceExpression GetReference()
        {
            return new ArmParameterReferenceExpression(this);
        }

        IArmString IArmReferenceable.ReferenceName => Name;

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new ArmParameter((IArmString)Name.Instantiate(parameters), (IArmString)Type.Instantiate(parameters)), parameters);

        public override string ToString()
        {
            return GetReference().ToString();
        }
    }

    public class ArmParameter<T> : ArmParameter
    {
        public ArmParameter(IArmString name) : base(name, GetArmType())
        {
        }

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new ArmParameter<T>((IArmString)Name.Instantiate(parameters)), parameters);

        private static IArmString GetArmType()
        {
            if (!ArmTypeConversion.TryConvertToArmType(typeof(T), out ArmType? armType))
            {
                throw new ArgumentException($"The type '{typeof(T)}' is not a valid ARM parameter type");
            }

            return armType.Value.AsArmString();
        }
    }
}

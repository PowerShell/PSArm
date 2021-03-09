
// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using PSArm.Types;
using System;
using System.Security;

namespace PSArm.Templates
{
    public class ArmParameter : ArmObject, IArmReferenceable<ArmParameterReferenceExpression>
    {
        public static explicit operator ArmParameterReferenceExpression(ArmParameter parameter) => parameter.GetReference();

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

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitParameterDeclaration(this);

        public ArmParameterReferenceExpression GetReference()
        {
            string typeName = Type.CoerceToString();
            if (!ArmTypeConversion.TryConvertToArmType(typeName, out ArmType? armType))
            {
                throw new InvalidOperationException($"Cannot create reference for ARM parameter of invalid type '{typeName}'");
            }

            switch (armType)
            {
                case ArmType.String:
                    return new ArmParameterReferenceExpression<string>(this);

                case ArmType.SecureString:
                    return new ArmParameterReferenceExpression<SecureString>(this);

                case ArmType.Int:
                    return new ArmParameterReferenceExpression<int>(this);

                case ArmType.Bool:
                    return new ArmParameterReferenceExpression<bool>(this);

                case ArmType.Object:
                    return new ArmParameterReferenceExpression<object>(this);

                case ArmType.SecureObject:
                    return new ArmParameterReferenceExpression<SecureObject>(this);

                case ArmType.Array:
                    return new ArmParameterReferenceExpression<Array>(this);

                default:
                    throw new InvalidOperationException($"Cannot create reference for ARM parameter of unsupported type '{armType}'");
            }
        }

        IArmString IArmReferenceable.ReferenceName => Name;
    }

    public class ArmParameter<T> : ArmParameter
    {
        public ArmParameter(IArmString name) : base(name, GetArmType())
        {
        }

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

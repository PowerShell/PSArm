
// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    internal static class ArmTemplateKeys
    {
        internal static ArmStringLiteral ApiVersion { get; } = new ArmStringLiteral(nameof(ApiVersion).UnPascal());

        internal static ArmStringLiteral Type { get; } = new ArmStringLiteral(nameof(Type).UnPascal());

        internal static ArmStringLiteral Name { get; } = new ArmStringLiteral(nameof(Name).UnPascal());

        internal static ArmStringLiteral Location { get; } = new ArmStringLiteral(nameof(Location).UnPascal());

        internal static ArmStringLiteral Kind { get; } = new ArmStringLiteral(nameof(Kind).UnPascal());

        internal static ArmStringLiteral Properties { get; } = new ArmStringLiteral(nameof(Properties).UnPascal());

        internal static ArmStringLiteral Resources { get; } = new ArmStringLiteral(nameof(Resources).UnPascal());

        internal static ArmStringLiteral Sku { get; } = new ArmStringLiteral(nameof(Sku).UnPascal());

        internal static ArmStringLiteral DependsOn { get; } = new ArmStringLiteral(nameof(DependsOn).UnPascal());

        internal static ArmStringLiteral Tier { get; } = new ArmStringLiteral(nameof(Tier).UnPascal());

        internal static ArmStringLiteral Size { get; } = new ArmStringLiteral(nameof(Size).UnPascal());

        internal static ArmStringLiteral Family { get; } = new ArmStringLiteral(nameof(Family).UnPascal());

        internal static ArmStringLiteral Capacity { get; } = new ArmStringLiteral(nameof(Capacity).UnPascal());

        internal static ArmStringLiteral Value { get; } = new ArmStringLiteral(nameof(Value).UnPascal());

        internal static ArmStringLiteral DefaultValue { get; } = new ArmStringLiteral(nameof(DefaultValue).UnPascal());

        internal static ArmStringLiteral AllowedValues { get; } = new ArmStringLiteral(nameof(AllowedValues).UnPascal());

        internal static ArmStringLiteral Schema { get; } = new ArmStringLiteral("$schema");

        internal static ArmStringLiteral ContentVersion { get; } = new ArmStringLiteral(nameof(ContentVersion).UnPascal());

        internal static ArmStringLiteral Outputs { get; } = new ArmStringLiteral(nameof(Outputs).UnPascal());

        internal static ArmStringLiteral Parameters { get; } = new ArmStringLiteral(nameof(Parameters).UnPascal());

        internal static ArmStringLiteral Variables { get; } = new ArmStringLiteral(nameof(Variables).UnPascal());
    }
}

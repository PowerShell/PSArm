
// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    internal static class ArmTemplateKeys
    {
        internal static ArmStringLiteral ApiVersion { get; } = new ArmStringLiteral(nameof(ApiVersion).CamelCase());

        internal static ArmStringLiteral Type { get; } = new ArmStringLiteral(nameof(Type).CamelCase());

        internal static ArmStringLiteral Name { get; } = new ArmStringLiteral(nameof(Name).CamelCase());

        internal static ArmStringLiteral Location { get; } = new ArmStringLiteral(nameof(Location).CamelCase());

        internal static ArmStringLiteral Kind { get; } = new ArmStringLiteral(nameof(Kind).CamelCase());

        internal static ArmStringLiteral Properties { get; } = new ArmStringLiteral(nameof(Properties).CamelCase());

        internal static ArmStringLiteral Resources { get; } = new ArmStringLiteral(nameof(Resources).CamelCase());

        internal static ArmStringLiteral Sku { get; } = new ArmStringLiteral(nameof(Sku).CamelCase());

        internal static ArmStringLiteral DependsOn { get; } = new ArmStringLiteral(nameof(DependsOn).CamelCase());

        internal static ArmStringLiteral Tier { get; } = new ArmStringLiteral(nameof(Tier).CamelCase());

        internal static ArmStringLiteral Size { get; } = new ArmStringLiteral(nameof(Size).CamelCase());

        internal static ArmStringLiteral Family { get; } = new ArmStringLiteral(nameof(Family).CamelCase());

        internal static ArmStringLiteral Capacity { get; } = new ArmStringLiteral(nameof(Capacity).CamelCase());

        internal static ArmStringLiteral Value { get; } = new ArmStringLiteral(nameof(Value).CamelCase());

        internal static ArmStringLiteral DefaultValue { get; } = new ArmStringLiteral(nameof(DefaultValue).CamelCase());

        internal static ArmStringLiteral AllowedValues { get; } = new ArmStringLiteral(nameof(AllowedValues).CamelCase());

        internal static ArmStringLiteral Schema { get; } = new ArmStringLiteral("$schema");

        internal static ArmStringLiteral ContentVersion { get; } = new ArmStringLiteral(nameof(ContentVersion).CamelCase());

        internal static ArmStringLiteral Outputs { get; } = new ArmStringLiteral(nameof(Outputs).CamelCase());

        internal static ArmStringLiteral Parameters { get; } = new ArmStringLiteral(nameof(Parameters).CamelCase());

        internal static ArmStringLiteral Variables { get; } = new ArmStringLiteral(nameof(Variables).CamelCase());

        internal static ArmStringLiteral Metadata { get; } = new ArmStringLiteral(nameof(Metadata).CamelCase());

        internal static ArmStringLiteral Comments { get; } = new ArmStringLiteral(nameof(Comments).CamelCase());

        internal static ArmStringLiteral GeneratorKey { get; } = new ArmStringLiteral("_generator");

        internal static ArmStringLiteral Version { get; } = new ArmStringLiteral(nameof(Version).CamelCase());

        internal static ArmStringLiteral TemplateHash { get; } = new ArmStringLiteral(nameof(TemplateHash).CamelCase());

        internal static ArmStringLiteral Metadata_PSVersion { get; } = new ArmStringLiteral("psarm-psversion");

        internal static ArmStringLiteral Template { get; } = new ArmStringLiteral(nameof(Template).CamelCase());

        internal static ArmStringLiteral Mode { get; } = new ArmStringLiteral(nameof(Mode).CamelCase());

        internal static ArmStringLiteral ExpressionEvaluationOptions { get; } = new ArmStringLiteral(nameof(ExpressionEvaluationOptions).CamelCase());

        internal static ArmStringLiteral Scope { get; } = new ArmStringLiteral(nameof(Scope).CamelCase());
    }
}

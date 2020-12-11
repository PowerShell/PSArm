using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    internal static class ArmTemplateKeys
    {
        internal static ArmStringLiteral ApiVersion { get; } = new ArmStringLiteral(nameof(ApiVersion));

        internal static ArmStringLiteral Type { get; } = new ArmStringLiteral(nameof(Type));

        internal static ArmStringLiteral Name { get; } = new ArmStringLiteral(nameof(Name));

        internal static ArmStringLiteral Location { get; } = new ArmStringLiteral(nameof(Location));

        internal static ArmStringLiteral Kind { get; } = new ArmStringLiteral(nameof(Kind));

        internal static ArmStringLiteral Properties { get; } = new ArmStringLiteral(nameof(Properties));

        internal static ArmStringLiteral Resources { get; } = new ArmStringLiteral(nameof(Resources));

        internal static ArmStringLiteral Sku { get; } = new ArmStringLiteral(nameof(Sku));

        internal static ArmStringLiteral DependsOn { get; } = new ArmStringLiteral(nameof(DependsOn));

        internal static ArmStringLiteral Tier { get; } = new ArmStringLiteral(nameof(Tier));

        internal static ArmStringLiteral Size { get; } = new ArmStringLiteral(nameof(Size));

        internal static ArmStringLiteral Family { get; } = new ArmStringLiteral(nameof(Family));

        internal static ArmStringLiteral Capacity { get; } = new ArmStringLiteral(nameof(Capacity));

        internal static ArmStringLiteral Value { get; } = new ArmStringLiteral(nameof(Value));

        internal static ArmStringLiteral DefaultValue { get; } = new ArmStringLiteral(nameof(DefaultValue));

        internal static ArmStringLiteral AllowedValues { get; } = new ArmStringLiteral(nameof(AllowedValues));

        internal static ArmStringLiteral Schema { get; } = new ArmStringLiteral(nameof(Schema));

        internal static ArmStringLiteral ContentVersion { get; } = new ArmStringLiteral(nameof(ContentVersion));

        internal static ArmStringLiteral Outputs { get; } = new ArmStringLiteral(nameof(Outputs));

        internal static ArmStringLiteral Parameters { get; } = new ArmStringLiteral(nameof(Parameters));

        internal static ArmStringLiteral Variables { get; } = new ArmStringLiteral(nameof(Variables));
    }
}

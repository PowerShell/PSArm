using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    internal static class ArmTemplateKeys
    {
        internal static ArmStringValue ApiVersion { get; } = new ArmStringValue(nameof(ApiVersion));

        internal static ArmStringValue Type { get; } = new ArmStringValue(nameof(Type));

        internal static ArmStringValue Name { get; } = new ArmStringValue(nameof(Name));

        internal static ArmStringValue Location { get; } = new ArmStringValue(nameof(Location));

        internal static ArmStringValue Kind { get; } = new ArmStringValue(nameof(Kind));

        internal static ArmStringValue Properties { get; } = new ArmStringValue(nameof(Properties));

        internal static ArmStringValue Resources { get; } = new ArmStringValue(nameof(Resources));

        internal static ArmStringValue Sku { get; } = new ArmStringValue(nameof(Sku));

        internal static ArmStringValue DependsOn { get; } = new ArmStringValue(nameof(DependsOn));

        internal static ArmStringValue Tier { get; } = new ArmStringValue(nameof(Tier));

        internal static ArmStringValue Size { get; } = new ArmStringValue(nameof(Size));

        internal static ArmStringValue Family { get; } = new ArmStringValue(nameof(Family));

        internal static ArmStringValue Capacity { get; } = new ArmStringValue(nameof(Capacity));

        internal static ArmStringValue Value { get; } = new ArmStringValue(nameof(Value));

        internal static ArmStringValue DefaultValue { get; } = new ArmStringValue(nameof(DefaultValue));

        internal static ArmStringValue AllowedValues { get; } = new ArmStringValue(nameof(AllowedValues));

        internal static ArmStringValue Schema { get; } = new ArmStringValue(nameof(Schema));

        internal static ArmStringValue ContentVersion { get; } = new ArmStringValue(nameof(ContentVersion));

        internal static ArmStringValue Outputs { get; } = new ArmStringValue(nameof(Outputs));

        internal static ArmStringValue Parameters { get; } = new ArmStringValue(nameof(Parameters));

        internal static ArmStringValue Variables { get; } = new ArmStringValue(nameof(Variables));
    }
}

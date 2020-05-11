using System.Collections.Generic;
using System.Management.Automation;
using PSArm.Expression;

namespace PSArm.Commands.Expression
{
    [Alias("ResourceId")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinResourceId")]
    public class ArmBuiltinResourceIdCommand : ArmBuiltinCommand
    {
        public ArmBuiltinResourceIdCommand() : base("resourceId")
        {
        }

        [ValidateNotNull]
        [Parameter]
        public IArmExpression SubscriptionId { get; set; }

        [ValidateNotNull]
        [Parameter]
        public IArmExpression ResourceGroupName { get; set; }

        [ValidateNotNull]
        [Parameter(Position = 0, Mandatory = true)]
        public IArmExpression ResourceType { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 1, Mandatory = true, ValueFromRemainingArguments = true)]
        public IArmExpression[] ResourceName { get; set; }

        protected override IArmExpression[] GetArguments()
        {
            var args = new List<IArmExpression>();

            if (SubscriptionId != null)
            {
                args.Add(SubscriptionId);
            }

            if (ResourceGroupName != null)
            {
                args.Add(ResourceGroupName);
            }

            args.Add(ResourceType);
            args.AddRange(ResourceName);

            return args.ToArray();
        }
    }

}
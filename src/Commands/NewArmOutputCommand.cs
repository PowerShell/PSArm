using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Expression;

namespace PSArm.Commands
{
    [Alias("Output")]
    [Cmdlet(VerbsCommon.New, "ArmOutput")]
    public class NewArmOutputCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public IArmExpression Name { get; set; }

        [Parameter(Mandatory = true)]
        public IArmExpression Type { get; set; }

        [Parameter(Mandatory = true)]
        public IArmExpression Value { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(new ArmOutput
            {
                Name = Name,
                Type = Type,
                Value = Value,
            });
        }
    }
}
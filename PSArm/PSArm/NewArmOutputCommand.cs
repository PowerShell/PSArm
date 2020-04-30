using System.Management.Automation;

namespace PSArm
{
    [Alias("Output")]
    [Cmdlet(VerbsCommon.New, "ArmOutput")]
    public class NewArmOutputCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string Name { get; set; }

        [Parameter(Mandatory = true)]
        public string Type { get; set; }

        [Parameter(Mandatory = true)]
        public object Value { get; set; }

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
using System.Management.Automation;

namespace PSArm
{
    [Alias("Resource")]
    [Cmdlet(VerbsCommon.New, "ArmResource")]
    public class NewArmResourceCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter()]
        public string Location { get; set; }

        [Parameter()]
        public string ApiVersion { get; set; }

        [Parameter()]
        public string Type { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock Body { get; set; }
    }
}
using System.Management.Automation;

namespace PSArm
{
    public abstract class PassthruCommand : PSCmdlet
    {
        public abstract ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            foreach (PSObject result in InvokeCommand.InvokeScript(SessionState, Body))
            {
                WriteObject(result);
            }
        }
    }

    [Alias("Properties")]
    [Cmdlet(VerbsCommon.New, "ArmProperties")]
    public class NewArmPropertiesCommand : PassthruCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public override ScriptBlock Body { get; set; }
    }
}
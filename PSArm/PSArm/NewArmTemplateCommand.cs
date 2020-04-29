using System;
using System.Management.Automation;

namespace PSArm
{
    [Alias("Arm")]
    [Cmdlet(VerbsCommon.New, "ArmTemplate")]
    public class NewArmTemplateCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            var armTemplate = new ArmTemplate();
            foreach (PSObject item in InvokeCommand.InvokeScript(SessionState, Body))
            {
                if (item.BaseObject is ArmResource armResource)
                {
                    armTemplate.Resources.Add(armResource);
                }
            }
            WriteObject(armTemplate);
        }
    }
}

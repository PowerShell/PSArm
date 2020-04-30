using System;
using System.Management.Automation;

namespace PSArm
{
    [Alias("Arm")]
    [Cmdlet(VerbsCommon.New, "ArmTemplate")]
    public class NewArmTemplateCommand : PSCmdlet
    {
        private static Version s_defaultVersion = new Version(1, 0, 0, 0);

        [Parameter()]
        public Version ContentVersion { get; set; } = s_defaultVersion;

        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            var armTemplate = new ArmTemplate()
            {
                ContentVersion = ContentVersion,
            };
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


// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Templates;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Template")]
    public class NewPSArmTemplateCommand : PSArmKeywordCommand
    {
        internal const string KeywordName = "Arm";

        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            WriteArmObjectElement<ArmTemplate>(Body);
        }
    }
}

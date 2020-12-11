using PSArm.Commands.Internal;
using PSArm.Completion;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Template
{
    [Alias("Resource")]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Resource")]
    public class NewPSArmResourceCommand : PSArmKeywordCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public IArmString Name { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter(Mandatory = true)]
        public IArmString ApiVersion { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter(Mandatory = true)]
        public IArmString Provider { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter(Mandatory = true)]
        public IArmString Type { get; set; }

        [Parameter]
        public IArmString Location { get; set; }

        [Parameter]
        public IArmString Kind { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public ScriptBlock Body { get; set; }
    }
}

using PSArm.Commands.Internal;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Template
{
    [Alias("Properties")]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Properties")]
    public class NewPSArmPropertiesCommand : PassthruCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public override ScriptBlock Body { get; set; }
    }
}

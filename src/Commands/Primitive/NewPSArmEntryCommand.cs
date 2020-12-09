using PSArm.Commands.Internal;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Primitive
{
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Entry")]
    public class NewPSArmEntryCommand : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public IArmString Key { get; set; }
    }
}

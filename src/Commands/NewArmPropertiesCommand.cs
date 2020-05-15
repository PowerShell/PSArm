
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;

namespace PSArm.Commands
{

    [Alias("Properties")]
    [Cmdlet(VerbsCommon.New, "ArmProperties")]
    public class NewArmPropertiesCommand : PassthruCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public override ScriptBlock Body { get; set; }
    }
}

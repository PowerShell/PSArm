
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Expression;

namespace PSArm.Commands.ArmBuilding
{
    [Alias("Value")]
    [Cmdlet(VerbsCommon.New, "ArmValue")]
    public class NewArmValueCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public IArmExpression Value { get; set; }

        protected override void EndProcessing()
        {
            WriteObject(new ArmPropertyValue(Name, Value));
        }
    }
}

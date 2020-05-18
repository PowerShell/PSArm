
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;
using PSArm.Expression;

namespace PSArm.Commands
{
    [Alias("Sku")]
    [Cmdlet(VerbsCommon.New, "ArmSku")]
    public class NewArmSkuCommand : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public IArmExpression Name { get; set; }
    }
}
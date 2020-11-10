
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Expression;

namespace PSArm.Commands
{
    [Alias("Sku")]
    [Cmdlet(VerbsCommon.New, "ArmSku")]
    public class NewArmSkuCommand : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public IArmExpression Name { get; set; }

        [Parameter]
        public IArmExpression Tier { get; set; }

        [Parameter]
        public IArmExpression Size { get; set; }

        [Parameter]
        public IArmExpression Family { get; set; }

        [Parameter]
        public IArmExpression Capacity { get; set; }

        protected override void EndProcessing()
        {
            WriteObject(
                new ArmSku
                {
                    Name = Name,
                    Tier = Tier,
                    Size = Size,
                    Family = Family,
                    Capacity = Capacity,
                });
        }
    }
}
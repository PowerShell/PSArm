using PSArm.Commands.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Template
{
    [Alias("Sku")]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Sku")]
    public class NewPSArmSkuCommand : PSArmKeywordCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public IArmString Name { get; set; }

        [Parameter]
        public IArmString Tier { get; set; }

        [Parameter]
        public IArmString Size { get; set; }

        [Parameter]
        public IArmString Family { get; set; }

        [Parameter]
        public IArmString Capacity { get; set; }

        protected override void EndProcessing()
        {
            WriteArmValueEntry(
                ArmTemplateKeys.Sku,
                new ArmSku
                {
                    Name = Name,
                    Tier = Tier,
                    Size = Size,
                    Family = Family,
                    Capacity = Capacity
                });
        }
    }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
    [OutputType(typeof(ArmEntry))]
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Sku")]
    public class NewPSArmSkuCommand : PSArmKeywordCommand
    {
        internal const string KeywordName = "ArmSku";

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

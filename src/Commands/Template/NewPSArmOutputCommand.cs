
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
    [OutputType(typeof(ArmEntry))]
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Output")]
    public class NewPSArmOutputCommand : PSArmKeywordCommand
    {
        internal const string KeywordName = "Output";

        [Parameter(Position = 0, Mandatory = true)]
        public IArmString Name { get; set; }

        [Parameter(Mandatory = true)]
        public IArmString Type { get; set; }

        [Parameter(Mandatory = true)]
        public IArmString Value { get; set; }

        protected override void EndProcessing()
        {
            WriteArmValueEntry(
                ArmTemplateKeys.Outputs,
                new ArmObject
                {
                    [Name] = new ArmOutput(Name) { Type = Type, Value = Value }
                });
        }
    }
}

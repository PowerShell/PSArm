using PSArm.Commands.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Template
{
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
                Name, 
                new ArmOutput(Name)
                {
                    Type = Type,
                    Value = Value,
                });
        }
    }
}

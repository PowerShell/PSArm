using PSArm.Commands.Internal;
using PSArm.Templates.Builders;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Primitive
{
    [Alias(Name)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Entry", DefaultParameterSetName = "Value")]
    public class NewPSArmEntryCommand : PSArmKeywordCommand
    {
        internal const string Name = "RawEntry";

        [Parameter(Mandatory = true, Position = 0)]
        public IArmString Key { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Value")]
        public ArmElement Value { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Body")]
        public ScriptBlock Body { get; set; }

        [Parameter]
        public SwitchParameter Array { get; set; }

        [Parameter(ParameterSetName = "Body")]
        public SwitchParameter ArrayBody { get; set; }

        protected override void EndProcessing()
        {
            if (Value != null)
            {
                WriteArmValueEntry(Key, Value, isArrayElement: Array);
                return;
            }

            if (ArrayBody)
            {
                WriteArmArrayEntry<ArmArray>(Key, Body, isArrayElement: Array);
                return;
            }

            WriteArmObjectEntry<ArmObject>(Key, Body, isArrayElement: Array);
        }
    }
}

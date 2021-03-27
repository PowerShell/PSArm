
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Templates.Primitives;
using System.Management.Automation;

namespace PSArm.Commands.Primitive
{
    [OutputType(typeof(ArmElement))]
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Element", DefaultParameterSetName = "Value")]
    public class NewPSArmElementCommand : PSArmKeywordCommand
    {
        internal const string KeywordName = "ArmElement";

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Value")]
        public ArmElement Value { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Body")]
        public ScriptBlock Body { get; set; }

        [Parameter(ParameterSetName = "Body")]
        public SwitchParameter ArrayBody { get; set; }

        protected override void EndProcessing()
        {
            if (Value != null)
            {
                WriteObject(Value);
                return;
            }

            if (ArrayBody)
            {
                WriteArmArrayElement(Body);
                return;
            }

            WriteArmObjectElement(Body);
        }
    }
}

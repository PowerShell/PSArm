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

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Body_Object")]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Body_Array")]
        public ScriptBlock Body { get; set; }

        [Parameter]
        public SwitchParameter Array { get; set; }

        [Parameter(ParameterSetName = "Body_Array")]
        public SwitchParameter ArrayBody { get; set; }

        [Parameter(ParameterSetName = "Body_Object")]
        [ValidateNotNullOrEmpty]
        public string DiscriminatorKey { get; set; }

        [Parameter(ParameterSetName = "Body_Object")]
        [ValidateNotNullOrEmpty]
        public string DiscriminatorValue { get; set; }

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

            var armBuilder = new ConstructingArmBuilder<ArmObject>();

            if (DiscriminatorKey is not null || DiscriminatorValue is not null)
            {
                if (DiscriminatorKey is null)
                {
                    ThrowArgumentError(nameof(DiscriminatorKey));
                    return;
                }

                if (DiscriminatorValue is null)
                {
                    ThrowArgumentError(nameof(DiscriminatorValue));
                    return;
                }

                armBuilder.AddSingleElement(new ArmStringLiteral(DiscriminatorKey), new ArmStringLiteral(DiscriminatorValue));
            }

            WriteArmObjectEntry(armBuilder, Key, Body, isArrayElement: Array);
        }

        private void ThrowArgumentError(string argumentName)
        {
            ThrowTerminatingError(
                new ErrorRecord(
                    new ArgumentException($"The parameter '{argumentName}' must be provided"),
                    "MissingRequiredParameter",
                    ErrorCategory.ObjectNotFound,
                    targetObject: this));
        }
    }
}

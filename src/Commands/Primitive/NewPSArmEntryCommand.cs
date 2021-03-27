
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Templates.Builders;
using PSArm.Templates.Primitives;
using System;
using System.Management.Automation;

namespace PSArm.Commands.Primitive
{
    [OutputType(typeof(ArmEntry))]
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Entry", DefaultParameterSetName = "Value")]
    public class NewPSArmEntryCommand : PSArmKeywordCommand
    {
        internal const string KeywordName = "RawEntry";

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

        [Parameter(ParameterSetName = "Body")]
        [ValidateNotNullOrEmpty]
        public string DiscriminatorKey { get; set; }

        [Parameter(ParameterSetName = "Body")]
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
            this.ThrowTerminatingError(
                new ArgumentException($"The parameter '{argumentName}' must be provided"),
                "MissingRequiredParameter",
                ErrorCategory.ObjectNotFound,
                target: this);
        }
    }
}

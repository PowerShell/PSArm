
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System.Management.Automation;

namespace PSArm.Commands.Primitive
{
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "FunctionCall")]
    public class NewPSArmFunctionCallCommand : Cmdlet
    {
        public const string KeywordName = "RawCall";

        [Parameter(Mandatory = true, Position = 0)]
        public IArmString Name { get; set; }

        [Parameter(ValueFromRemainingArguments = true)]
        public ArmExpression[] Arguments { get; set; }

        protected override void EndProcessing()
        {
            WriteObject(new ArmFunctionCallExpression(Name, Arguments));
        }
    }
}

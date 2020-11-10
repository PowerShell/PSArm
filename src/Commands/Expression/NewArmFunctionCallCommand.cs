using PSArm.Expression;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Expression
{
    [Alias("Call")]
    [Cmdlet(VerbsCommon.New, "ArmFunctionCall")]
    public class NewArmFunctionCallCommand : ArmCallCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string FunctionName
        {
            get => Function;
            set => Function = value;
        }

        [Parameter(ValueFromRemainingArguments = true)]
        public IArmExpression[] Arguments { get; set; }
    }
}

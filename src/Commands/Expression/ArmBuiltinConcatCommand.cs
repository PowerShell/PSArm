using System.Management.Automation;
using PSArm.Expression;

namespace PSArm.Commands.Expression
{
    [Alias("Concat")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinConcat")]
    public class ArmBuiltinConcatCommand : ArmBuiltinCommand
    {
        public ArmBuiltinConcatCommand() : base("concat")
        {
        }

        [ValidateNotNullOrEmpty]
        [Parameter(ValueFromRemainingArguments = true)]
        public IArmExpression[] Arguments { get; set; }

        protected override void EndProcessing()
        {
            WriteObject(new ArmConcatCall(Arguments));
        }
    }

}
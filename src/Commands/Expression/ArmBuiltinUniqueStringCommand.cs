using System.Management.Automation;
using PSArm.Expression;

namespace PSArm.Commands.Expression
{
    [Alias("UniqueString")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinUniqueString")]
    public class InvokeArmBuiltinUniqueString : ArmBuiltinCommand
    {
        public InvokeArmBuiltinUniqueString() : base("uniqueString")
        {
        }

        [Parameter(Mandatory = true, Position = 0, ValueFromRemainingArguments = true)]
        public IArmExpression[] Input { get; set; }

        protected override IArmExpression[] GetArguments()
        {
            return Input;
        }
    }
}
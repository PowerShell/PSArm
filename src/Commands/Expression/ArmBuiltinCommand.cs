using System.Management.Automation;
using PSArm.Expression;

namespace PSArm.Commands.Expression
{
    public abstract class ArmBuiltinCommand : Cmdlet
    {
        protected ArmBuiltinCommand(string function)
        {
            Function = function;
        }

        public string Function { get; }

        protected virtual IArmExpression[] GetArguments()
        {
            return null;
        }

        protected override void EndProcessing()
        {
            WriteObject(new ArmFunctionCall(Function, GetArguments()));
        }
    }
}
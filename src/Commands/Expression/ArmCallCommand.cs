using PSArm.Expression;
using System.Management.Automation;

namespace PSArm.Commands.Expression
{
    public abstract class ArmCallCommand : Cmdlet
    {
        protected string Function { get; set; }

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

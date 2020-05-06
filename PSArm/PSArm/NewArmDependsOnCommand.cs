using System.Management.Automation;

namespace PSArm
{
    [Alias("DependsOn")]
    [Cmdlet(VerbsCommon.New, "ArmDependsOn", DefaultParameterSetName = "Value")]
    public class NewArmDependsOnCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Value", ValueFromPipeline = true)]
        public IArmExpression[] Value { get; set; }

        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Body")]
        public ScriptBlock Body { get; set; }

        protected override void ProcessRecord()
        {
            if (Value != null)
            {
                foreach (IArmExpression val in Value)
                {
                    WriteObject(new ArmDependsOn(val));
                }

                return;
            }

            if (Body != null)
            {
                foreach (PSObject result in InvokeCommand.InvokeScript(SessionState, Body))
                {
                    if (result.BaseObject == null)
                    {
                        continue;
                    }

                    try
                    {
                        WriteObject(new ArmDependsOn(ArmTypeConversion.Convert(result.BaseObject)));
                    }
                    catch
                    {
                        // Do nothing
                    }
                }
            }
        }
    }
}
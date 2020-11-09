
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Expression;

namespace PSArm.Commands
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
                        WriteObject(new ArmDependsOn((IArmExpression)ArmTypeConversion.Convert(result.BaseObject)));
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
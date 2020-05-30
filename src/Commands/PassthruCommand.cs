
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;

namespace PSArm.Commands
{
    public abstract class PassthruCommand : PSCmdlet
    {
        public abstract ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            foreach (PSObject result in InvokeCommand.InvokeScript(SessionState, Body))
            {
                WriteObject(result);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Internal
{
    public abstract class PassthruCommand : PSArmKeywordCommand
    {
        public abstract ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            foreach (PSObject result in InvokeBody(Body))
            {
                WriteObject(result);
            }
        }
    }
}

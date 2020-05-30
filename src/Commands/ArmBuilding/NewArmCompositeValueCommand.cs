
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Expression;

namespace PSArm.Commands.ArmBuilding
{
    [Alias("Composite")]
    [Cmdlet(VerbsCommon.New, "ArmCompositeValue")]
    public class NewArmCompositeValue : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public Dictionary<string, object> Parameters { get; set; }

        protected override void EndProcessing()
        {
            var result = new ArmParameterizedProperty(Name);
            foreach (KeyValuePair<string, object> parameter in Parameters)
            {
                result.Parameters[parameter.Key] = ArmTypeConversion.Convert(parameter.Value);
            }
            WriteObject(result);
        }
    }

}
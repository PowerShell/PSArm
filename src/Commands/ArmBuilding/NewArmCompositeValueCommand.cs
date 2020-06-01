
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections;
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

        [Parameter()]
        public Hashtable Parameters { get; set; }

        [Parameter()]
        public Hashtable Properties { get; set; }

        protected override void EndProcessing()
        {
            var result = new ArmParameterizedProperty(Name);
            foreach (DictionaryEntry parameter in Parameters)
            {
                result.Parameters[parameter.Key.ToString()] = ArmTypeConversion.Convert(parameter.Value);
            }
            if (Properties != null)
            {
                foreach (DictionaryEntry property in Properties)
                {
                    result.Parameters[property.Key.ToString()] = ArmTypeConversion.Convert(property.Value);
                }
            }
            WriteObject(result);
        }
    }

}
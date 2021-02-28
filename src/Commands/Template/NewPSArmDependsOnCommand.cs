
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "DependsOn", DefaultParameterSetName = "Value")]
    public class NewPSArmDependsOnCommand : PSArmKeywordCommand
    {
        internal const string KeywordName = "DependsOn";

        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Value")]
        public IArmString[] Value { get; set; }

        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Body")]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            var array = new ArmArray();

            if (Value != null)
            {
                foreach (IArmString value in Value)
                {
                    array.Add((ArmElement)value);
                }
                WriteArmValueEntry(ArmTemplateKeys.DependsOn, array);
                return;
            }

            foreach (PSObject output in InvokeBody(Body))
            {
                if (output is IArmString armString)
                {
                    array.Add((ArmElement)armString);
                }
            }

            WriteArmValueEntry(ArmTemplateKeys.DependsOn, array);
        }
    }
}

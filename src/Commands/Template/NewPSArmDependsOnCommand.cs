
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Internal;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
    [OutputType(typeof(ArmEntry))]
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
                if (LanguagePrimitives.TryConvertTo(output, typeof(IArmString), out object result))
                {
                    array.Add((ArmElement)result);
                }
            }

            WriteArmValueEntry(ArmTemplateKeys.DependsOn, array);
        }
    }
}

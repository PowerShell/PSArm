
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Internal;
using PSArm.Execution;
using PSArm.Parameterization;
using PSArm.Templates;
using PSArm.Templates.Builders;
using PSArm.Templates.Primitives;
using System;
using System.IO;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
    [OutputType(typeof(ArmTemplate))]
    [Alias(KeywordName)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Template")]
    public class NewPSArmTemplateCommand : PSArmKeywordCommand
    {
        internal const string KeywordName = "Arm";

        [Parameter]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            string templateName = Name;
            if (templateName is null)
            {
                try
                {
                    templateName = Path.GetFileName(MyInvocation.ScriptName);
                    templateName = templateName.Substring(0, templateName.Length - PSArmTemplateExecutor.PSArmFileExtension.Length);
                }
                catch
                {
                    // If we fail, just proceed with templateName = null
                }
            }

            // Create the ARM template in an alias-free environment
            ArmTemplate template = null;
            using (PSAliasContext.EnterCleanAliasContext(SessionState))
            {
                ScriptBlock transformedBody;
                ArmObject<ArmParameter> armParameters;
                ArmObject<ArmVariable> armVariables;
                object[] psArgsArray;

                using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
                {
                    try
                    {
                        transformedBody = new TemplateScriptBlockTransformer(pwsh).GetDeparameterizedTemplateScriptBlock(
                            Body,
                            out armParameters,
                            out armVariables,
                            out psArgsArray);
                    }
                    catch (Exception e)
                    {
                        this.ThrowTerminatingError(e, "TemplateScriptBlockTransformationFailure", ErrorCategory.InvalidArgument, Body);
                        return;
                    }
                }

                template = new ArmTemplate(templateName);

                if (armParameters is not null && armParameters.Count > 0)
                {
                    template.Parameters = armParameters;
                }

                if (armVariables is not null && armVariables.Count > 0)
                {
                    template.Variables = armVariables;
                }

                var templateBuilder = new ArmBuilder<ArmTemplate>(template);
                foreach (PSObject output in InvokeCommand.InvokeScript(useLocalScope: true, transformedBody, input: null, psArgsArray))
                {
                    if (output.BaseObject is ArmEntry armEntry)
                    {
                        templateBuilder.AddEntry(armEntry);
                    }
                }
            }

            WriteObject(template);
        }
    }
}

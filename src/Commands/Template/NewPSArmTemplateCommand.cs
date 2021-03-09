
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Execution;
using PSArm.Templates;
using PSArm.Templates.Builders;
using PSArm.Templates.Primitives;
using System;
using System.IO;
using System.Management.Automation;

namespace PSArm.Commands.Template
{
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

            ScriptBlock transformedBody;
            ArmObject<ArmParameter> armParameters;
            ArmObject<ArmVariable> armVariables;

            using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                try
                {
                    transformedBody = new TemplateScriptBlockTransformer(pwsh).GetDeparameterizedTemplateScriptBlock(
                        Body,
                        out armParameters,
                        out armVariables);
                }
                catch (Exception e)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            e,
                            "TemplateScriptBlockTransformationFailure",
                            ErrorCategory.InvalidArgument,
                            Body));
                    return;
                }
            }

            ArmTemplate template = AggregateArmObject(new ArmBuilder<ArmTemplate>(new ArmTemplate(templateName)), transformedBody);

            template.Parameters = armParameters;
            template.Variables = armVariables;

            WriteObject(template);
        }
    }
}


// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Serialization;
using PSArm.Templates;
using System;
using System.Management.Automation;

namespace PSArm.Commands
{
    [OutputType(typeof(ArmTemplate))]
    [Cmdlet(VerbsData.ConvertFrom, ModuleConstants.ModulePrefix + "JsonTemplate")]
    public class ConvertFromArmTemplateCommand : PSCmdlet
    {
        private readonly ArmParser _parser;

        private int _templateCount;

        public ConvertFromArmTemplateCommand()
        {
            _parser = new ArmParser();
            _templateCount = 0;
        }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "Path", Position = 0, Mandatory = true)]
        public string[] Path { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "Uri", Position = 0, Mandatory = true)]
        public Uri[] Uri { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "Input", Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string[] Input { get; set; }

        protected override void ProcessRecord()
        {
            _templateCount++;
            switch (ParameterSetName)
            {
                case "Input":
                    foreach (string input in Input)
                    {
                        WriteObject(_parser.ParseString(templateName: $"template_{_templateCount}", input));
                    }
                    return;

                case "Path":
                    foreach (string path in Path)
                    {
                        WriteObject(_parser.ParseFile(path));
                    }
                    return;

                case "Uri":
                    foreach (Uri uri in Uri)
                    {
                        WriteObject(_parser.ParseUri(uri));
                    }
                    return;
            }
        }
    }
}

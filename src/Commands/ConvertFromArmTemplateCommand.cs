
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Internal;
using PSArm.Serialization;
using PSArm.Templates;
using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace PSArm.Commands
{
    [OutputType(typeof(ArmTemplate))]
    [Cmdlet(VerbsData.ConvertFrom, "ArmTemplate")]
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
                    ProviderInfo provider = null;
                    foreach (string path in Path)
                    {
                        foreach (string resolvedPath in SessionState.Path.GetResolvedProviderPathFromPSPath(path, out provider))
                        {
                            WriteObject(_parser.ParseFile(resolvedPath));
                        }
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

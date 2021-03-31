
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Internal;
using PSArm.Internal;
using PSArm.Serialization;
using PSArm.Templates;
using System;
using System.IO;
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
        [SupportsWildcards]
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
                    foreach (string wildcardPath in Path)
                    {
                        foreach (string path in GetResolvedProviderPathFromPSPath(wildcardPath, out ProviderInfo provider))
                        {
                            if (!provider.Name.Is("FileSystem"))
                            {
                                WriteError(
                                    new ErrorRecord(
                                        new IOException($"Cannot convert non-filesystem template path '{path}'"),
                                        "BadProviderPath",
                                        ErrorCategory.InvalidArgument,
                                        path));
                                continue;
                            }

                            if (!File.Exists(path))
                            {
                                WriteError(
                                    new ErrorRecord(
                                        new FileNotFoundException($"ARM template file '{path}' does not exist"),
                                        "TemplateFileNotFound",
                                        ErrorCategory.ResourceUnavailable,
                                        path));
                                continue;
                            }

                            WriteObject(_parser.ParseFile(path));
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

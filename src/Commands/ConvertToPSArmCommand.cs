
// Copyright (c) Microsoft Corporation.

using PSArm.Commands.Internal;
using PSArm.Serialization;
using PSArm.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands
{
    [Cmdlet(VerbsData.ConvertTo, ModuleConstants.ModulePrefix)]
    public class ConvertToPSArmCommand : PSCmdlet
    {
        private List<ArmTemplate> _templatesToConvert;

        [ValidateNotNull]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ArmTemplate[] InputTemplate { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "OutFile")]
        public string OutFile { get; set; }

        [Parameter(ParameterSetName = "OutFile")]
        public SwitchParameter PassThru { get; set; }

        [Parameter(ParameterSetName = "OutFile")]
        public SwitchParameter Force { get; set; }

        public ConvertToPSArmCommand()
        {
            _templatesToConvert = new List<ArmTemplate>();
        }

        protected override void ProcessRecord()
        {
            foreach (ArmTemplate template in InputTemplate)
            {
                _templatesToConvert.Add(template);
            }
        }

        protected override void EndProcessing()
        {
            bool outFileSpecified = OutFile is not null;
            if (outFileSpecified)
            {
                string outFile = null;
                bool isDirectory = false;
                try
                {
                    outFile = GetOutputPath(out isDirectory);
                }
                catch (IOException e)
                {
                    this.ThrowTerminatingError(
                        e,
                        "UnableToUseOutFile",
                        ErrorCategory.ResourceExists,
                        OutFile);
                }

                FileMode writeMode;
                if (!isDirectory)
                {
                    if (!Force)
                    {
                        this.ThrowTerminatingError(
                            new IOException($"Cannot overwrite existing file '{outFile}'. Use '{nameof(Force)}' to override this."),
                            "OutFileExists",
                            ErrorCategory.ResourceExists,
                            OutFile);
                    }

                    File.Delete(outFile);
                    writeMode = FileMode.Append;
                }
                else
                {
                    writeMode = Force ? FileMode.Create : FileMode.CreateNew;
                }

                foreach (ArmTemplate template in _templatesToConvert)
                {
                    string outPath = isDirectory ? Path.Combine(outFile, $"{template.TemplateName}.json") : outFile;
                    PSArmWritingVisitor.WriteToFile(outPath, template, writeMode);
                }
            }

            if (!outFileSpecified || PassThru)
            {
                foreach (ArmTemplate template in _templatesToConvert)
                {
                    WriteObject(PSArmWritingVisitor.WriteToString(template));
                }
            }
        }

        private string GetOutputPath(out bool isDirectory)
        {
            string outFile = GetUnresolvedProviderPathFromPSPath(OutFile);
            bool fileExists = FileExists(outFile, out isDirectory);

            // If the file exists, we'll let Force determine what happens later
            if (fileExists)
            {
                return outFile;
            }

            // Otherwise we'll try to create a directory
            string fileExtension = Path.GetExtension(outFile);
            if (string.IsNullOrEmpty(fileExtension))
            {
                Directory.CreateDirectory(outFile);
                isDirectory = true;
                return outFile;
            }

            // Finally if we're absolutely being forced to write to a file,
            // we need to make sure the directory it lives in exists
            string dir = Path.GetDirectoryName(outFile);

            if (!FileExists(dir, out bool isDirDirectory))
            {
                Directory.CreateDirectory(dir);
                return outFile;
            }

            if (isDirDirectory)
            {
                return outFile;
            }

            // We have a file but need a directory,
            // so either clobber or fail

            if (Force)
            {
                WriteWarning($"OutFile parent directory '{dir}' already exists as a file and will be overwritten.");
                File.Delete(dir);
                Directory.CreateDirectory(dir);
                return outFile;
            }

            throw new IOException($"Cannot overwrite file '{dir}' to create directory for output path '{outFile}'. Please use the '{nameof(Force)}' parameter to override this.");
        }

        private bool FileExists(string path, out bool isDirectory)
        {
            try
            {
                FileAttributes fileAttrs = File.GetAttributes(path);

                isDirectory = (fileAttrs & FileAttributes.Directory) != 0;
                return true;
            }
            catch
            {
                isDirectory = false;
                return false;
            }
        }
    }
}

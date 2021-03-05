
// Copyright (c) Microsoft Corporation.

using PSArm.Templates;
using PSArm.Templates.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace PSArm.Execution
{
    internal class PSArmTemplateExecutor
    {
        public class Builder
        {
            private readonly List<string> _paths;

            public Builder()
            {
                _paths = new List<string>();
            }

            public Builder AddTemplatePath(string path)
            {
                _paths.Add(path);
                return this;
            }

            public Builder AddTemplatePaths(IEnumerable<string> paths)
            {
                _paths.AddRange(paths);
                return this;
            }

            public PSArmTemplateExecutor Build(PowerShell pwsh)
            {
                return new PSArmTemplateExecutor(pwsh, _paths);
            }
        }

        private const int MaxTemplateDirectoryDepth = 32;

        internal const string PSArmFileExtension = ".PSArm.ps1";

        private readonly IReadOnlyList<string> _templatePaths;

        private readonly ArmNestedTemplateBuilder _nestedTemplateBuilder;

        private readonly PowerShell _pwsh;

        protected PSArmTemplateExecutor(
            PowerShell pwsh,
            IReadOnlyList<string> templatePaths)
        {
            _templatePaths = templatePaths;
            _nestedTemplateBuilder = new ArmNestedTemplateBuilder();
            _pwsh = pwsh;
        }

        public ArmNestedTemplate EvaluatePSArmTemplates(IDictionary parameters)
        {
            _nestedTemplateBuilder.Clear();
            EvaluateAndCollectPSArmTemplates(_templatePaths, parameters, currDepth: 0);
            return _nestedTemplateBuilder.Build();
        }

        private void EvaluateAndCollectPSArmTemplates(IEnumerable<string> templatePaths, IDictionary parameters, int currDepth)
        {
            foreach (string templatePath in templatePaths)
            {
                EvaluateAndCollectPSArmTemplate(templatePath, parameters, currDepth);
            }
        }

        private void EvaluateAndCollectPSArmTemplate(string templatePath, IDictionary parameters, int currDepth)
        {
            if (currDepth > MaxTemplateDirectoryDepth)
            {
                throw new InvalidOperationException($"PSArm template evaluation reached maximum depth at path '{templatePath}'");
            }

            FileAttributes fileAttrs;
            try
            {
                // If the file does not exist at all, this will throw an IOException
                fileAttrs = File.GetAttributes(templatePath);
            }
            catch (IOException)
            {
                // If we were unable to read the file, continue on
                return;
            }

            // If the file is a directory, recursively enumerate the files
            if ((fileAttrs & FileAttributes.Directory) != 0)
            {
                EvaluateAndCollectPSArmTemplates(Directory.EnumerateFileSystemEntries(templatePath), parameters, currDepth + 1);
                return;
            }

            // If we have a file, but it doesn't end with our file extension, ignore it
            if (!templatePath.EndsWith(PSArmFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (ArmTemplate template in EvaluatePSArmTemplateScript(templatePath, parameters))
            {
                _nestedTemplateBuilder.AddTemplate(template);
            }
        }

        private IEnumerable<ArmTemplate> EvaluatePSArmTemplateScript(string scriptPath, IDictionary parameters)
        {
            _pwsh.Commands.Clear();

            _pwsh.AddCommand(scriptPath, useLocalScope: true);

            if (parameters is not null)
            {
                _pwsh.AddParameters(parameters);
            }

            foreach (PSObject result in _pwsh.Invoke())
            {
                if (_pwsh.HadErrors)
                {
                    throw new RuntimeException($"Errors occurred running script '{scriptPath}'. Template creation stopped.");
                }

                if (result.BaseObject is ArmTemplate template)
                {
                    yield return template;
                }
            }
        }
    }
}

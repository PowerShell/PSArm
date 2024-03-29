
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates;
using PSArm.Templates.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;

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

        public ArmNestedTemplate EvaluatePSArmTemplates(IDictionary parameters, CancellationToken cancellationToken)
        {
            _nestedTemplateBuilder.Clear();
            cancellationToken.Register(() => _pwsh.Stop());
            EvaluateAndCollectPSArmTemplates(_templatePaths, parameters, currDepth: 0, cancellationToken);
            return _nestedTemplateBuilder.Build();
        }

        private void EvaluateAndCollectPSArmTemplates(IEnumerable<string> templatePaths, IDictionary parameters, int currDepth, CancellationToken cancellationToken)
        {
            foreach (string templatePath in templatePaths)
            {
                EvaluateAndCollectPSArmTemplate(templatePath, parameters, currDepth, cancellationToken);
            }
        }

        private void EvaluateAndCollectPSArmTemplate(string templatePath, IDictionary parameters, int currDepth, CancellationToken cancellationToken)
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
                EvaluateAndCollectPSArmTemplates(Directory.EnumerateFileSystemEntries(templatePath), parameters, currDepth + 1, cancellationToken);
                return;
            }

            // If we have a file, but it doesn't end with our file extension, ignore it
            if (!templatePath.EndsWith(PSArmFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (ArmTemplate template in EvaluatePSArmTemplateScript(templatePath, parameters, cancellationToken))
            {
                _nestedTemplateBuilder.AddTemplate(template);
            }
        }

        private IEnumerable<ArmTemplate> EvaluatePSArmTemplateScript(string scriptPath, IDictionary parameters, CancellationToken cancellationToken)
        {
            _pwsh.Commands.Clear();

            _pwsh.AddCommand(scriptPath, useLocalScope: true);

            if (parameters is not null)
            {
                IDictionary scriptParameters = GetScriptParameters(scriptPath, parameters);
                _pwsh.AddParameters(scriptParameters);
            }

            foreach (PSObject result in _pwsh.Invoke())
            {
                if (_pwsh.HadErrors || _pwsh.InvocationStateInfo.State == PSInvocationState.Stopped)
                {
                    break;
                }

                if (result.BaseObject is ArmTemplate template)
                {
                    yield return template;
                }
            }

            if (_pwsh.HadErrors)
            {
                ErrorRecord error = _pwsh.Streams.Error[0];
                throw new TemplateExecutionException(error);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private IDictionary GetScriptParameters(string scriptPath, IDictionary parameters)
        {
            Ast ast = Parser.ParseFile(scriptPath, out Token[] _, out ParseError[] _);
            var paramAst = (ParamBlockAst)ast.Find((subAst) => subAst is ParamBlockAst, searchNestedScriptBlocks: false);

            var outputParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (paramAst?.Parameters is not null)
            {
                // Ensure case-sensitive hashtable inputs still work
                parameters = new Hashtable(parameters, StringComparer.OrdinalIgnoreCase);
                foreach (ParameterAst parameter in paramAst.Parameters)
                {
                    string parameterName = parameter.Name.VariablePath.UserPath;
                    if (parameters.Contains(parameterName))
                    {
                        outputParameters[parameterName] = parameters[parameterName];
                    }
                }
            }

            return outputParameters;
        }
    }
}

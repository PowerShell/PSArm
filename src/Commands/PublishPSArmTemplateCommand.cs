
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSArm.Commands.Internal;
using PSArm.Execution;
using PSArm.Templates;
using PSArm.Templates.Metadata;
using PSArm.Templates.Primitives;
using PSArm.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSArm.Commands
{
    [OutputType(typeof(ArmNestedTemplate))]
    [Cmdlet(VerbsData.Publish, ModuleConstants.ModulePrefix + "Template")]
    public class PublishPSArmTemplateCommand : PSCmdlet
    {
        private const string DefaultTemplateName = "template.json";

        private const string TemplateSigningApiUri = "https://management.azure.com/providers/Microsoft.Resources/calculateTemplateHash?api-version=2020-06-01";

        private readonly PSArmTemplateExecutor.Builder _templateExecutorBuilder;

        private readonly CancellationTokenSource _cancellationSource;

        public PublishPSArmTemplateCommand()
        {
            _templateExecutorBuilder = new PSArmTemplateExecutor.Builder();
            _cancellationSource = new CancellationTokenSource();
        }

        [SupportsWildcards]
        [Alias("Path")]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] TemplatePath { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter]
        public string AzureToken { get; set; }

        [Parameter]
        [HashtableTransformation]
        public Hashtable Parameters { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter]
        public string OutFile { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter NoWriteFile { get; set; }

        [Parameter]
        public SwitchParameter NoHashTemplate { get; set; }

        private bool Verbose => MyInvocation.BoundParameters.ContainsKey("Verbose");

        protected override void BeginProcessing()
        {
            if (!NoWriteFile)
            {
                string outPath = GetOutPath();
                if (!Force && File.Exists(outPath))
                {
                    this.ThrowTerminatingError(
                        new IOException($"File '{outPath}' already exists. Use the -{nameof(Force)} switch to overwrite it."),
                        "TemplateAlreadyExists",
                        ErrorCategory.ResourceExists,
                        OutFile);
                }
            }
        }

        protected override void ProcessRecord()
        {
            foreach (string path in TemplatePath)
            {
                if (_cancellationSource.IsCancellationRequested)
                {
                    break;
                }

                Collection<string> paths = GetResolvedProviderPathFromPSPath(path, out ProviderInfo provider);

                if (provider.Name != "FileSystem")
                {
                    this.ThrowTerminatingError(
                        new InvalidOperationException($"Cannot read PSArm file from non-filesystem provider '{provider}' from path '{path}'"),
                        "InvalidPSArmProviderPath",
                        ErrorCategory.InvalidArgument,
                        path);
                    return;
                }

                _templateExecutorBuilder.AddTemplatePaths(paths);
            }
        }

        protected override void EndProcessing()
        {
            IReadOnlyDictionary<IArmString, ArmElement> armParameters = null;
            try
            {
                armParameters = GetTemplateParameters();
            }
            catch (Exception e)
            {
                this.ThrowTerminatingError(
                    e,
                    "TemplateParameterConversionError",
                    ErrorCategory.InvalidArgument,
                    Parameters);
            }

            ArmNestedTemplate aggregatedTemplate = null;
            using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                WriteVerbose("Building template executor");
                PSArmTemplateExecutor templateExecutor = _templateExecutorBuilder.Build(pwsh);
                try
                {
                    WriteVerbose("Finding and evaluating templates");
                    aggregatedTemplate = templateExecutor.EvaluatePSArmTemplates(Parameters, _cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (TemplateExecutionException templateException)
                {
                    ThrowTerminatingError(templateException.ErrorRecord);
                    return;
                }
                catch (RuntimeException runtimeException)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(runtimeException.ErrorRecord, runtimeException));
                    return;
                }
                catch (InvalidOperationException invalidOpException)
                {
                    this.ThrowTerminatingError(invalidOpException, "TemplateEvaluationError", ErrorCategory.InvalidOperation, TemplatePath);
                    return;
                }
            }

            // Now instantiate the template
            aggregatedTemplate = (ArmNestedTemplate)aggregatedTemplate.Instantiate(armParameters);

            // Set the PowerShell version for telemetry
            string psVersion = ((Hashtable)SessionState.PSVariable.GetValue("PSVersionTable"))["PSVersion"].ToString();
            ((PSArmTopLevelTemplateMetadata)aggregatedTemplate.Metadata).GeneratorMetadata.PowerShellVersion = new ArmStringLiteral(psVersion);

            try
            {
                aggregatedTemplate = RunIOOperationsAsync(aggregatedTemplate, _cancellationSource.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (HttpRequestException httpException)
            {
                this.ThrowTerminatingError(
                    httpException,
                    "TemplateHashRequestFailed",
                    ErrorCategory.ConnectionError);
            }
            catch (IOException ioException)
            {
                this.ThrowTerminatingError(
                    ioException,
                    "TemplateFileWriteFailed",
                    ErrorCategory.WriteError,
                    GetOutPath());
            }
            catch (Exception e)
            {
                this.ThrowTerminatingError(
                    e,
                    "TemplateCreationFailed",
                    ErrorCategory.InvalidOperation);
            }

            if (PassThru)
            {
                WriteObject(aggregatedTemplate);
            }
        }

        protected override void StopProcessing()
        {
            _cancellationSource.Cancel();
        }

        private async Task<ArmNestedTemplate> RunIOOperationsAsync(ArmNestedTemplate template, CancellationToken cancellationToken)
        {
            if (!NoHashTemplate)
            {
                SafeWriteVerbose("Signing template");
                template = await AddHashToTemplate(template, cancellationToken).ConfigureAwait(false);
            }

            if (!NoWriteFile)
            {
                await WriteTemplate(template, cancellationToken).ConfigureAwait(false);
            }

            return template;
        }

        private async Task WriteTemplate(ArmNestedTemplate template, CancellationToken cancellationToken)
        {
            string outPath = GetOutPath();

            FileMode writeMode = Force ? FileMode.Create : FileMode.CreateNew;

            using var file = new FileStream(outPath, writeMode, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var writer = new StreamWriter(file, Encoding.UTF8);
            using var jsonWriter = new JsonTextWriter(writer)
            {
                Formatting = Formatting.Indented,
            };

            SafeWriteVerbose($"Writing template to '{outPath}'");

            await template.ToJson().WriteToAsync(jsonWriter, cancellationToken).ConfigureAwait(false);
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private async Task<ArmNestedTemplate> AddHashToTemplate(ArmNestedTemplate template, CancellationToken cancellationToken)
        {
            SafeWriteVerbose("Getting Azure token");
            string token = GetAzureToken(cancellationToken);

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            using var jsonWriter = new JsonTextWriter(writer);

            using var httpClient = Verbose
                ? new HttpClient(new VerboseHttpLoggingHandler(Host.UI, new HttpClientHandler()))
                : new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await template.ToJson().WriteToAsync(jsonWriter, cancellationToken).ConfigureAwait(false);
            await jsonWriter.FlushAsync(cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);

            var body = new StreamContent(stream)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                    {
                        CharSet = "utf-8"
                    },
                },
            };

            SafeWriteVerbose("Initiating HTTP request");
            using HttpResponseMessage response = await httpClient.PostAsync(TemplateSigningApiUri, body, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            Stream responseBody = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            using var streamReader = new StreamReader(responseBody);
            using var jsonReader = new JsonTextReader(streamReader);
            string hash = await GetHashFromJsonResponse(jsonReader, cancellationToken).ConfigureAwait(false);

            SafeWriteVerbose($"Adding hash '{hash}' to template");
            ((PSArmTopLevelTemplateMetadata)template.Metadata).GeneratorMetadata.TemplateHash = new ArmStringLiteral(hash);

            return template;
        }

        private async Task<string> GetHashFromJsonResponse(JsonTextReader jsonReader, CancellationToken cancellationToken)
        {
            JObject body = await JObject.LoadAsync(jsonReader, cancellationToken).ConfigureAwait(false);

            return ((body["templateHash"] as JValue)?.Value as string) ?? throw new InvalidOperationException($"Did not get template hash value from signing API");
        }

        private IReadOnlyDictionary<IArmString, ArmElement> GetTemplateParameters()
        {
            if (Parameters is null)
            {
                return null;
            }

            var parameters = new Dictionary<IArmString, ArmElement>();
            foreach (DictionaryEntry entry in Parameters)
            {
                if (!ArmElementConversion.TryConvertToArmString(entry.Key, out IArmString key))
                {
                    throw new ArgumentException($"Cannot convert hashtable key '{entry.Key}' of type '{entry.Key.GetType()}' to ARM string");
                }

                if (!ArmElementConversion.TryConvertToArmElement(entry.Value, out ArmElement value))
                {
                    if (entry.Value is SecureString)
                    {
                        throw new ArgumentException($"Cannot provide secure string values for template publication. Provide these at template deployment time");
                    }

                    throw new ArgumentException($"Cannot convert hashtable value '{entry.Value}' of type '{entry.Value.GetType()}' to ARM element");
                }

                parameters[key] = value;
            }

            return parameters;
        }

        private string GetAzureToken(CancellationToken cancellationToken)
        {
            // If we have a token given, just use that
            if (AzureToken is not null)
            {
                return AzureToken;
            }

            // Now try falling back to Azure tools
            string token = null;
            bool hasAzPS = true;
            bool hasAzCli = true;
            using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                cancellationToken.Register(() => pwsh.Stop());

                // First try Azure PowerShell's Get-AzAccessToken
                SafeWriteVerbose("Attempting to get Azure token with 'Get-AzAccessToken'");
                try
                {
                    token = pwsh
                        .AddCommand("Get-AzAccessToken")
                        .Invoke()
                        .Select(psobj => (string)psobj.Properties["Token"].Value)
                        .FirstOrDefault();
                }
                catch (CommandNotFoundException)
                {
                    hasAzPS = false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!pwsh.HadErrors && token is not null)
                {
                    return token;
                }

                // Next try the az CLI
                pwsh.Commands.Clear();
                
                SafeWriteVerbose("Attempting to get Azure token with 'az account get-access-token'");
                try
                {
                    token = pwsh
                        .AddCommand("az")
                            .AddArgument("account")
                            .AddArgument("get-access-token")
                        .AddCommand("ConvertFrom-Json")
                        .Invoke()
                        .Select(psobj => (string)psobj.Properties["accessToken"].Value)
                        .FirstOrDefault();
                }
                catch (CommandNotFoundException)
                {
                    hasAzCli = false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!pwsh.HadErrors && token is not null)
                {
                    return token;
                }

                if (hasAzPS)
                {
                    ThrowUnableToGetTokenError(
                        $"Unable to automatically get Azure token. Run 'Login-AzAccount' to login and enable token acquisition, or provide a token manually with the {nameof(AzureToken)} parameter");
                }

                if (hasAzCli)
                {
                    ThrowUnableToGetTokenError(
                        $"Unable to automatically get Azure token. Run 'az login' to login and enable token acquisition, or provide a token manually with the {nameof(AzureToken)} parameter");
                }

                ThrowUnableToGetTokenError(
                    $"Unable to automatically get Azure token. Install either Azure PowerShell or the az CLI and login to enable token acquisition, or provide a token manually with the {nameof(AzureToken)} parameter");

                return null;
            }
        }

        private void ThrowUnableToGetTokenError(string message)
        {
            this.ThrowTerminatingError(
                new ArgumentException($"Unable to automatically acquire Azure token: '{message}"),
                "NoAzToken",
                ErrorCategory.PermissionDenied);
        }

        private string GetOutPath()
        {
            string pwd = SessionState.Path.CurrentFileSystemLocation.Path;

            string path;
            if (OutFile is null)
            {
                path = Path.Combine(pwd, DefaultTemplateName);
            }
            else if (Path.IsPathRooted(OutFile))
            {
                path = OutFile;
            }
            else
            {
                path = Path.Combine(pwd, OutFile);
            }

            return Path.GetFullPath(path);
        }

        private void SafeWriteVerbose(string message)
        {
            if (Verbose)
            {
                Host.UI.WriteVerboseLine(message);
            }
        }
    }
}

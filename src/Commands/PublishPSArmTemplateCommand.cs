
// Copyright (c) Microsoft Corporation.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSArm.Commands.Internal;
using PSArm.Execution;
using PSArm.Templates;
using PSArm.Templates.Metadata;
using PSArm.Templates.Primitives;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PSArm.Commands
{
    [Cmdlet(VerbsData.Publish, ModuleConstants.ModulePrefix + "Template")]
    public class PublishPSArmTemplateCommand : PSCmdlet
    {
        private const string DefaultTemplateName = "template.json";

        private const string TemplateSigningApiUri = "https://management.azure.com/providers/Microsoft.Resources/calculateTemplateHash?api-version=2020-06-01?api-version=2020-06-01";

        private readonly PSArmTemplateExecutor.Builder _templateExecutorBuilder;

        public PublishPSArmTemplateCommand()
        {
            _templateExecutorBuilder = new PSArmTemplateExecutor.Builder();
        }

        [SupportsWildcards]
        [Alias("Path")]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] TemplatePath { get; set; }

        [Parameter()]
        public string AzureToken { get; set; }

        [Parameter]
        public Hashtable Parameters { get; set; }

        [Parameter]
        public string OutFile { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter NoWriteFile { get; set; }

        protected override void ProcessRecord()
        {
            foreach (string path in TemplatePath)
            {
                Collection<string> paths = GetResolvedProviderPathFromPSPath(path, out ProviderInfo provider);

                if (provider.Name != "FileSystem")
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new InvalidOperationException($"Cannot read PSArm file from non-filesystem provider '{provider}' from path '{path}'"),
                            "InvalidPSArmProviderPath",
                            ErrorCategory.InvalidArgument,
                            path));
                    return;
                }

                _templateExecutorBuilder.AddTemplatePaths(paths);
            }
        }

        protected override void EndProcessing()
        {
            ArmNestedTemplate aggregatedTemplate = null;
            using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                PSArmTemplateExecutor templateExecutor = _templateExecutorBuilder.Build(pwsh);
                try
                {
                    aggregatedTemplate = templateExecutor.EvaluatePSArmTemplates();
                }
                catch (InvalidOperationException e)
                {
                    ThrowTerminatingError(new ErrorRecord(e, "TemplateEvaluationError", ErrorCategory.InvalidOperation, TemplatePath));
                    return;
                }
            }

            try
            {
                aggregatedTemplate = RunIOOperationsAsync(aggregatedTemplate).GetAwaiter().GetResult();
            }
            catch (HttpRequestException httpException)
            {
                ThrowTerminatingError(
                    httpException,
                    "TemplateHashRequestFailed",
                    ErrorCategory.ConnectionError);
            }
            catch (IOException ioException)
            {
                ThrowTerminatingError(
                    ioException,
                    "TemplateFileWriteFailed",
                    ErrorCategory.WriteError,
                    GetOutPath());
            }
            catch (Exception e)
            {
                ThrowTerminatingError(
                    e,
                    "TemplateCreationFailed",
                    ErrorCategory.InvalidOperation);
            }
        }

        private async Task<ArmNestedTemplate> RunIOOperationsAsync(ArmNestedTemplate template)
        {
            template = await SignTemplate(template).ConfigureAwait(false);

            if (!NoWriteFile)
            {
                await WriteTemplate(template).ConfigureAwait(false);
            }

            return template;
        }

        private async Task WriteTemplate(ArmNestedTemplate template)
        {
            string outPath = GetOutPath();

            using var streamWriter = new StreamWriter(outPath, append: false, Encoding.UTF8);
            using var jsonWriter = new JsonTextWriter(streamWriter);

            await template.ToJson().WriteToAsync(jsonWriter).ConfigureAwait(false);
        }

        private async Task<ArmNestedTemplate> SignTemplate(ArmNestedTemplate template)
        {
            string token = GetAzureToken();

            using var stream = new MemoryStream();
            using var textReader = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(textReader);
            using var httpClient = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", token),
                }
            };

            await template.ToJson().WriteToAsync(jsonWriter).ConfigureAwait(false);

            var body = new StreamContent(stream);

            HttpResponseMessage response = await httpClient.PostAsync(TemplateSigningApiUri, body).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            Stream responseBody = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            using var streamReader = new StreamReader(responseBody);
            using var jsonReader = new JsonTextReader(streamReader);
            string hash = await GetHashFromJsonResponse(jsonReader).ConfigureAwait(false);

            ((PSArmTopLevelTemplateMetadata)template.Metadata).GeneratorMetadata.TemplateHash = new ArmStringLiteral(hash);

            return template;
        }

        private async Task<string> GetHashFromJsonResponse(JsonTextReader jsonReader)
        {
            JObject body = await JObject.LoadAsync(jsonReader).ConfigureAwait(false);

            return ((body["templateHash"] as JValue)?.Value as string) ?? throw new InvalidOperationException($"Did not get template hash value from signing API");
        }

        private string GetAzureToken()
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
                // First try Azure PowerShell's Get-AzAccessToken
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

                if (!pwsh.HadErrors && token is not null)
                {
                    return token;
                }

                // Next try the az CLI
                pwsh.Commands.Clear();
                
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

                if (!pwsh.HadErrors && token is not null)
                {
                    return token;
                }

                if (hasAzPS)
                {
                    ThrowUnableToGetTokenError(
                        $"Run 'Login-AzAccount' to login and enable token acquisition, or provide a token manually with the {nameof(AzureToken)} parameter");
                }

                if (hasAzCli)
                {
                    ThrowUnableToGetTokenError(
                        $"Run 'az login' to login and enable token acquisition, or provide a token manually with the {nameof(AzureToken)} parameter");
                }

                ThrowUnableToGetTokenError(
                    $"Install either Azure PowerShell or the az CLI and login to enable token acquisition, or provide a token manually with the {nameof(AzureToken)} parameter");

                return null;
            }
        }

        private void ThrowUnableToGetTokenError(string message)
        {
            ThrowTerminatingError(
                new ArgumentException($"Unable to automatically acquire Azure token: '{message}"),
                "NoAzToken",
                ErrorCategory.PermissionDenied);
        }

        private string GetOutPath()
        {
            string pwd = SessionState.Path.CurrentFileSystemLocation.Path;

            if (OutFile is null)
            {
                return Path.Combine(pwd, DefaultTemplateName);
            }

            return Path.IsPathRooted(OutFile)
                ? OutFile
                : Path.Combine(pwd, OutFile);
        }

        private void ThrowTerminatingError(Exception e, string errorId, ErrorCategory errorCategory, object targetObject = null)
        {
            ThrowTerminatingError(new ErrorRecord(e, errorId, errorCategory, targetObject));
        }
    }
}

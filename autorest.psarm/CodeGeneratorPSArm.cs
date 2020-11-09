using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoRest.AzureResourceSchema;
using AutoRest.AzureResourceSchema.Models;
using AutoRest.Core;
using AutoRest.Core.Model;
using Newtonsoft.Json;

namespace AutoRest.PSArm
{
    using System.IO;

    public class CodeGeneratorPSArm : CodeGenerator
    {
        public override string ImplementationFileExtension => ".json";

        public override string UsageInstructions => "";

        public string OutputFolder { get; set; }
        
        public Logger Logger { get; set; }

        public override async Task Generate(CodeModel serviceClient)
        {
            IEnumerable<string> apiVersions = serviceClient.Methods
                .SelectMany(method => method.XMsMetadata.apiVersions)
                .Concat(new [] { serviceClient.ApiVersion })
                .Where(v => v != null)
                .Distinct();

            var schemaBuilder = new DslSchemaBuilder(Logger, serviceClient);

            foreach (string version in apiVersions)
            {
                foreach (KeyValuePair<string, ResourceSchema> resourceProvider in ResourceSchemaParser.Parse(serviceClient, version))
                {
                    Logger.Log($"Processing: '{resourceProvider.Key}'");
                    schemaBuilder.AddResourceProvider(resourceProvider.Key, version, resourceProvider.Value);
                }
            }

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            foreach ((string, string, ResourceProviderBuilder) resource in schemaBuilder.GetProviders())
            {
                string providerName = resource.Item1;
                string apiVersion = resource.Item2;
                ResourceProviderBuilder resourceBuilder = resource.Item3;

                if (resource.Item3.Keywords.Count == 0)
                {
                    Logger.Log($"Resource '{providerName}_{apiVersion}' has no keywords and will be skipped");
                    continue;
                }

                string outputPath = string.Join('/', OutputFolder, $"{providerName}_{apiVersion}.json");
                using (var writer = new StringWriter())
                using (var jsonWriter = new JsonTextWriter(writer){ Formatting = Formatting.Indented })
                {
                    Logger.Log($"Writing out: '{outputPath}'");
                    resourceBuilder.ToJson().WriteTo(jsonWriter);
                    await Write(writer.ToString(), outputPath);
                    Logger.Log($"Done.{Environment.NewLine}-------------------{Environment.NewLine}{Environment.NewLine}");
                }
            }
        }
    }
}

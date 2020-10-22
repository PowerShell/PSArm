using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoRest.AzureResourceSchema;
using AutoRest.AzureResourceSchema.Models;
using AutoRest.Core;
using AutoRest.Core.Model;
using AutoRest.Modeler.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AutoRest.PSArm
{
    using System.IO;

    public class CodeGeneratorPSArm : CodeGenerator
    {
        public override string ImplementationFileExtension => ".json";

        public override string UsageInstructions => "";

        public string OutputFolder { get; set; }

        public override async Task Generate(CodeModel serviceClient)
        {
            IEnumerable<string> apiVersions = serviceClient.Methods
                .SelectMany(method => method.XMsMetadata.apiVersions)
                .Concat(new [] { serviceClient.ApiVersion })
                .Where(v => v != null)
                .Distinct();

            var schemaBuilder = new DslSchemaBuilder(serviceClient);

            foreach (string version in apiVersions)
            {
                foreach (KeyValuePair<string, ResourceSchema> resourceProvider in ResourceSchemaParser.Parse(serviceClient, version))
                {
                    Console.Error.WriteLine($"Processing: '{resourceProvider.Key}'");
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
                string outputPath = Path.Combine(OutputFolder, $"{providerName}_{apiVersion}.json");
                using (var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(file))
                using (var jsonWriter = new JsonTextWriter(writer){ Formatting = Formatting.Indented })
                {
                    Console.Error.WriteLine($"Writing out: '{outputPath}'");
                    resource.Item3.ToJson().WriteTo(jsonWriter);
                }
            }
        }
    }
}

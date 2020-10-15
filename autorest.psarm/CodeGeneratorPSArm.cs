using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoRest.AzureResourceSchema;
using AutoRest.AzureResourceSchema.Models;
using AutoRest.Core;
using AutoRest.Core.Model;

namespace AutoRest.PSArm
{
    public class CodeGeneratorPSArm : CodeGenerator
    {
        public override string ImplementationFileExtension => ".json";

        public override string UsageInstructions => "";

        public override async Task Generate(CodeModel serviceClient)
        {
            IEnumerable<string> apiVersions = serviceClient.Methods
                .SelectMany(method => method.XMsMetadata.apiVersions)
                .Concat(new [] { serviceClient.ApiVersion })
                .Where(v => v != null)
                .Distinct();

            foreach (string version in apiVersions)
            {
                foreach (KeyValuePair<string, ResourceSchema> resourceProvider in ResourceSchemaParser.Parse(serviceClient, version))
                {
                    Console.Error.WriteLine($"Processing: '{resourceProvider.Key}'");
                }
            }
        }
    }
}

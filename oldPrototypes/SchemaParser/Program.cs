
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using Newtonsoft.Json;
using RobImpl.ArmSchema;
using System;
using System.Collections.Generic;
using System.Text;

namespace RobImpl
{
    public static class Program
    {
        /// <summary>
        /// Not required, but concisely represents the steps we need to generate our own keyword schema.
        /// Useful for quick testing.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            string rootUriStr = args != null && args.Length > 0
                ? args[0]
                : "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json";

            Uri rootUri = new Uri(rootUriStr);
            // We pull the top level schema down here
            ArmJsonSchema schema = new ArmSchemaBuildingVisitor().CreateFromHttpUri(rootUri);
            // Now we try to remove all the allOfs in the schema.
            // We take allOfs and merge them together, essentially like monomorphization.
            // We also flatten all the oneOfs so that oneOfs containing oneOfs are all merged.
            // This is tricky and currently where the implementation falls down
            schema = schema.Fold();
            // This next step is untested
            Dictionary<string, PropertyTable> propertyHierarchy = new PropertySchemaBuilder().BuildPropertyHierarchy((ArmObjectSchema)schema);
            Console.WriteLine("=== Hierarchy in JSON form ===");
            Console.WriteLine(JsonConvert.SerializeObject(propertyHierarchy));
        }
    }
}

using Newtonsoft.Json;
using RobImpl.ArmSchema;
using System;
using System.Collections.Generic;
using System.Text;

namespace RobImpl
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string rootUriStr = args != null && args.Length > 0
                ? args[0]
                : "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json";

            Uri rootUri = new Uri(rootUriStr);
            ArmJsonSchema schema = new ArmSchemaBuildingVisitor().CreateFromHttpUri(rootUri);
            schema = schema.Fold();
            Dictionary<string, PropertyTable> propertyHierarchy = new PropertySchemaBuilder().BuildPropertyHierarchy((ArmObjectSchema)schema);
            Console.WriteLine("=== Hierarchy in JSON form ===");
            Console.WriteLine(JsonConvert.SerializeObject(propertyHierarchy));
        }
    }
}


// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace PSArm.Schema
{
    public class PSArmDslFactory
    {
        internal const string DiscriminatedResourceFunctionName = "DefineDiscriminatedKeywords";

        public ResourceDslDefinition CreateResourceDslDefinition(
            IReadOnlyDictionary<string, TypeBase> resourceSchema,
            IReadOnlyDictionary<string, ITypeReference> discriminatedSubtypes)
        {
            var functionDictionary = CreateDslDefinitionFromSchema(resourceSchema);

            if (discriminatedSubtypes is null)
            {
                return new ResourceDslDefinition(functionDictionary);
            }

            // If we have discriminated subtypes for a resource,
            // define extra function contexts for those.
            // When invoked, these will then be combined before invocation based on the provided discriminator.
            var discriminatedFunctions = new Dictionary<string, Dictionary<string, ScriptBlock>>(discriminatedSubtypes.Count);
            foreach (KeyValuePair<string, ITypeReference> discriminatedSubtype in discriminatedSubtypes)
            {
                if (discriminatedSubtype.Value.Type is not ObjectType objectSchema)
                {
                    throw new ArgumentException($"Discriminated subtype expected to be of type '{typeof(ObjectType)}' but instead got '{discriminatedSubtype.Value.Type.GetType()}'");
                }

                discriminatedFunctions[discriminatedSubtype.Key] = CreateDslDefinitionFromSchema(objectSchema.Properties);
            }

            return new ResourceDslDefinition(functionDictionary, discriminatedFunctions);
        }

        private Dictionary<string, ScriptBlock> CreateDslDefinitionFromSchema(IReadOnlyDictionary<string, TypeBase> schemaProperties)
        {
            var functionDict = new Dictionary<string, ScriptBlock>();

            foreach (KeyValuePair<string, TypeBase> propertySchemaEntry in schemaProperties)
            {
                using (var sw = new StringWriter())
                {
                    var writer = new PSArmDslWriter(sw);
                    writer.WriteSchemaDefinition(propertySchemaEntry.Key, propertySchemaEntry.Value, out string functionName);
                    functionDict[functionName] = ScriptBlock.Create(sw.ToString());
                }
            }

            return functionDict;
        }

        private Dictionary<string, ScriptBlock> CreateDslDefinitionFromSchema(IDictionary<string, ObjectProperty> schemaProperties)
        {
            var functionDict = new Dictionary<string, ScriptBlock>();

            foreach (KeyValuePair<string, ObjectProperty> propertySchemaEntry in schemaProperties)
            {
                using (var sw = new StringWriter())
                {
                    var writer = new PSArmDslWriter(sw);
                    writer.WriteSchemaDefinition(propertySchemaEntry.Key, propertySchemaEntry.Value.Type.Type, out string functionName);
                    functionDict[functionName] = ScriptBlock.Create(sw.ToString());
                }
            }

            return functionDict;
        }
    }
}

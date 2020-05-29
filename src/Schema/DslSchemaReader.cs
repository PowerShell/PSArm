
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Resources;
using Newtonsoft.Json;

namespace PSArm.Schema
{
    /// <summary>
    /// Reader object to read ARM DSL schemas from files.
    /// </summary>
    public class DslSchemaReader
    {
        private const string Key_Resources = "$resources";
        private const string Key_Parameters = "parameters";
        private const string Key_PropertyParameters = "propertyParameters";
        private const string Key_Array = "array";
        private const string Key_Body = "body";
        private const string Key_Type = "type";
        private const string Key_Enum = "enum";

        private static readonly char[] s_schemaNamePartsSeparators = new [] { '_' };

        private Dictionary<string, ArmDslKeywordSchema> _keywordPointerCache;

        private Dictionary<string, ArmDslKeywordSchema> _keywordTable;

        /// <summary>
        /// Create a new DSL schema reader.
        /// </summary>
        public DslSchemaReader()
        {
            _keywordPointerCache = new Dictionary<string, ArmDslKeywordSchema>();
            _keywordTable = new Dictionary<string, ArmDslKeywordSchema>();
        }

        /// <summary>
        /// Read an ARM resource DSL schema from a given file.
        /// </summary>
        /// <param name="path">The path to the file to read from.</param>
        /// <returns>The DSL schema object the file describes.</returns>
        public ArmDslProviderSchema ReadProviderSchema(string path)
        {
            _keywordPointerCache.Clear();
            _keywordTable.Clear();

            var schemaDocument = JsonDocument.FromPath(path);

            string[] fileNameParts = Path.GetFileNameWithoutExtension(path).Split(s_schemaNamePartsSeparators);
            string providerName = fileNameParts[0];
            string apiVersion = fileNameParts[1];

            return ReadProviderSchema(providerName, apiVersion, (JsonObject)schemaDocument.Root);
        }

        private ArmDslProviderSchema ReadProviderSchema(string providerName, string apiVersion, JsonObject schemaObject)
        {
            var provider = new ArmDslProviderSchema(providerName, apiVersion);

            if (schemaObject.Fields.TryGetValue(Key_Resources, out JsonItem resources))
            {
                var resourcesObject = (JsonObject)resources;
                foreach (KeyValuePair<string, JsonItem> resourceField in resourcesObject.Fields)
                {
                    provider.Resources.Add(resourceField.Key, ReadResourceSchema(resourceField.Key, (JsonObject)resourceField.Value));
                }
            }

            provider.Keywords = _keywordTable;

            return provider;
        }

        private ArmDslResourceSchema ReadResourceSchema(string resourceType, JsonObject resourceObject)
        {
            var resource = new ArmDslResourceSchema(resourceType);

            foreach (KeyValuePair<string, JsonItem> keywordEntry in resourceObject.Fields)
            {
                resource.Keywords.Add(keywordEntry.Key, ReadKeywordSchema(keywordEntry.Key, (JsonPointer)keywordEntry.Value));
            }

            return resource;
        }

        private ArmDslKeywordSchema ReadKeywordSchema(string keywordName, JsonPointer kwPtr)
        {
            if (_keywordPointerCache.TryGetValue(kwPtr.ReferenceUri, out ArmDslKeywordSchema existingKeyword))
            {
                return existingKeyword;
            }

            var keywordSchema = (JsonObject)kwPtr.ResolvedItem;
            var keyword = new ArmDslKeywordSchema(keywordName);
            _keywordPointerCache[kwPtr.ReferenceUri] = keyword;
            _keywordTable[keywordName] = keyword;

            if (keywordSchema.Fields.TryGetValue(Key_Parameters, out JsonItem parameters))
            {
                foreach (KeyValuePair<string, JsonItem> parameter in ((JsonObject)parameters).Fields)
                {
                    keyword.Parameters.Add(parameter.Key, ReadParameterSchema(parameter.Key, (JsonObject)parameter.Value));
                }
            }

            if (keywordSchema.Fields.TryGetValue(Key_PropertyParameters, out JsonItem propertyParameters))
            {
                foreach (KeyValuePair<string, JsonItem> propertyParameter in ((JsonObject)propertyParameters).Fields)
                {
                    keyword.PropertyParameters.Add(propertyParameter.Key, ReadParameterSchema(propertyParameter.Key, (JsonObject)propertyParameter.Value));
                }
            }

            if (keywordSchema.Fields.TryGetValue(Key_Array, out JsonItem array))
            {
                keyword.Array = ((JsonBoolean)array).Value;
            }

            if (keywordSchema.Fields.TryGetValue(Key_Body, out JsonItem body))
            {
                var bodyKeywords = new Dictionary<string, ArmDslKeywordSchema>();
                foreach (KeyValuePair<string, JsonItem> nestedKeyword in ((JsonObject)body).Fields)
                {
                    bodyKeywords.Add(nestedKeyword.Key, ReadKeywordSchema(nestedKeyword.Key, (JsonPointer)nestedKeyword.Value));
                }
                keyword.Body = bodyKeywords;
            }

            return keyword;
        }

        private ArmDslParameterSchema ReadParameterSchema(string parameterName, JsonObject parameterSchema)
        {
            var parameter = new ArmDslParameterSchema(parameterName);

            if (parameterSchema.Fields.TryGetValue(Key_Type, out JsonItem typeValue))
            {
                parameter.Type = ((JsonString)typeValue).Value;
            }

            if (parameterSchema.Fields.TryGetValue(Key_Enum, out JsonItem enumValues))
            {
                var enums = new List<object>();
                foreach (JsonItem enumValue in ((JsonArray)enumValues).Items)
                {
                    enums.Add(((JsonValue)enumValue).ValueAsObject);
                }
                parameter.Enum = enums;
            }

            return parameter;
        }
    }
}

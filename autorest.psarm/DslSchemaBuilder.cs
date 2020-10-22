using AutoRest.AzureResourceSchema;
using AutoRest.AzureResourceSchema.Models;
using AutoRest.Core.Model;
using Newtonsoft.Json.Linq;
using PSArm.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AutoRest.PSArm
{
    public static class ValueKeywordExtensions
    {
        public static ValueKeyword<T> AddProperties<T>(
            this ValueKeyword<T> keyword,
            JsonSchema bodySchema,
            Func<string, T> parseValue)
        {
            if (bodySchema.Enum != null && bodySchema.Enum.Count > 0)
            {
                var allowedValues = new T[bodySchema.Enum.Count];
                for (int i = 0; i < bodySchema.Enum.Count; i++)
                {
                    allowedValues[i] = parseValue(bodySchema.Enum[i]);
                }
                keyword.Enum = allowedValues;
            }

            if (!string.IsNullOrEmpty(bodySchema.Default))
            {
                keyword.Default = parseValue(bodySchema.Default);
            }

            return keyword;
        }
    }

    public abstract class Keyword
    {
        public Keyword(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public abstract JObject ToJson();
    }

    public class BodyKeyword : Keyword
    {
        public BodyKeyword(string name) : base(name)
        {
            BlockKeywords = new Dictionary<string, KeywordReference>();
        }

        public Dictionary<string, KeywordReference> BlockKeywords { get; }

        public override JObject ToJson()
        {
            var body = new JObject();
            foreach (KeyValuePair<string, KeywordReference> subword in BlockKeywords)
            {
                body[subword.Key] = new JObject
                {
                    ["$ref"] = new JValue(subword.Value.DefinitionPath)
                };
            }
            return new JObject
            {
                ["body"] = body
            };
        }
    }

    public class ArrayKeyword : Keyword
    {
        public ArrayKeyword(string name) : base(name)
        {
        }

        public Keyword ElementSchema { get; set; }

        public override JObject ToJson()
        {
            JObject elementJson = ElementSchema.ToJson();
            elementJson["array"] = new JValue(true);
            return elementJson;
        }
    }

    public abstract class ValueKeyword<T> : Keyword
    {
        private bool _hasDefault;

        public ValueKeyword(string name) : base(name)
        {
            _hasDefault = false;
        }

        public T[] Enum { get; set; }

        public T Default { get; set; }

        public abstract string KeywordValueType { get; }

        public override JObject ToJson()
        {
            var valueObject = new JObject
            {
                ["type"] = KeywordValueType,
            };

            if (Enum != null)
            {
                valueObject["enum"] = new JArray(Enum.Select(v => new JValue(v)));
            }

            if (Default != null)
            {
                valueObject["default"] = new JValue(Default);
            }

            return new JObject
            {
                ["propertyParameters"] = new JObject
                {
                    ["value"] = valueObject
                }
            };
        }
    }

    public class StringKeyword : ValueKeyword<string>
    {
        public StringKeyword(string name) : base(name)
        {
        }

        public string Pattern { get; set; }

        public override string KeywordValueType => "string";
    }

    public class BooleanKeyword : ValueKeyword<bool>
    {
        public BooleanKeyword(string name) : base(name)
        {
        }

        public override string KeywordValueType => "bool";
    }

    public class IntegerKeyword : ValueKeyword<int>
    {
        public IntegerKeyword(string name) : base(name)
        {
        }

        public override string KeywordValueType => "int";
    }

    public class KeywordReference
    {
        public KeywordReference(string keywordId, string keyword)
        {
            Id = keywordId;
            Name = keyword;
            DefinitionPath = $"#/$keywords/{keywordId}";
        }

        public string Id { get; }

        public string Name { get; }

        public string DefinitionPath { get; }

        public Keyword Keyword { get; private set; }

        public void SetKeyword(Keyword keyword)
        {
            Keyword = keyword;
        }
    }

    public class KeywordTable
    {
        private readonly Dictionary<JsonSchema, KeywordReference> _keywordsBySchema;

        private readonly Dictionary<string, KeywordReference> _keywordsById;

        public KeywordTable()
        {
            _keywordsById = new Dictionary<string, KeywordReference>();
            _keywordsBySchema = new Dictionary<JsonSchema, KeywordReference>();
        }

        public KeywordReference GetKeyword(string keywordName, JsonSchema bodySchema, out bool alreadyExisted)
        {
            if (_keywordsBySchema.TryGetValue(bodySchema, out KeywordReference existingReference))
            {
                alreadyExisted = true;
                return existingReference;
            }

            string keywordId = keywordName;
            int i = 1;
            while (_keywordsById.ContainsKey(keywordId))
            {
                keywordId = $"{keywordId}_{i}";
                i++;
            }

            var keywordReference = new KeywordReference(keywordId, keywordName);
            _keywordsById[keywordId] = keywordReference;
            _keywordsBySchema[bodySchema] = keywordReference;
            alreadyExisted = false;
            return keywordReference;
        }

        public IEnumerable<KeyValuePair<string, KeywordReference>> GetKeywords()
        {
            return _keywordsById;
        }
    }

    public class ResourceProviderBuilder
    {
        private readonly CodeModel _codeModel;

        private readonly ResourceSchema _resourceSchema;

        private readonly KeywordTable _keywordTable;

        private readonly Dictionary<string, Dictionary<string, KeywordReference>> _typeKeywords;

        public ResourceProviderBuilder(
            CodeModel codeModel,
            ResourceSchema resourceSchema,
            string providerName,
            string apiVersion)
        {
            _codeModel = codeModel;
            _resourceSchema = resourceSchema;
            ProviderName = providerName;
            ApiVersion = apiVersion;
            _keywordTable = new KeywordTable();
            _typeKeywords = new Dictionary<string, Dictionary<string, KeywordReference>>();
        }

        public string ProviderName { get; }

        public string ApiVersion { get; }

        public JObject ToJson()
        {
            var keywords = new JObject();
            foreach (KeyValuePair<string, KeywordReference> keyword in _keywordTable.GetKeywords())
            {
                keywords[keyword.Key] = keyword.Value.Keyword.ToJson();
            }

            var resources = new JObject();
            foreach (KeyValuePair<string, Dictionary<string, KeywordReference>> resource in _typeKeywords)
            {
                var resourceObj = new JObject();
                foreach (KeyValuePair<string, KeywordReference> keyword in resource.Value)
                {
                    resourceObj[keyword.Key] = new JObject
                    {
                        ["$ref"] = keyword.Value.DefinitionPath
                    };
                }
                resources[resource.Key] = resourceObj;
            }

            return new JObject
            {
                ["$keywords"] = keywords,
                ["$resources"] = resources,
            };
        }

        public ResourceProviderBuilder AddResource(string resourceType, JsonSchema resourceSchema)
        {
            if (!_typeKeywords.TryGetValue(resourceType, out Dictionary<string, KeywordReference> resourceKeywords))
            {
                resourceKeywords = new Dictionary<string, KeywordReference>();
                _typeKeywords[resourceType] = resourceKeywords;
            }

            AddKeywords(resourceKeywords, resourceSchema);

            return this;
        }

        private void AddKeywords(Dictionary<string, KeywordReference> resourceKeywordTable, JsonSchema resourceJsonSchema)
        {
            if (!TryGetProperties(resourceJsonSchema, out JsonSchema propertiesSchema))
            {
                return;
            }

            foreach (KeyValuePair<string, JsonSchema> property in propertiesSchema.Properties)
            {
                resourceKeywordTable[property.Key] = GetKeywordFromProperty(property.Key, property.Value);
            }
        }

        private KeywordReference GetKeywordFromProperty(string keyword, JsonSchema keywordSchema)
        {
            KeywordReference keywordRef = _keywordTable.GetKeyword(keyword, keywordSchema, out bool alreadyExisted);

            if (alreadyExisted)
            {
                return keywordRef;
            }

            keywordRef.SetKeyword(GetKeywordBodyFromSchema(keywordRef.Name, keywordSchema));

            return keywordRef;
        }

        private Keyword GetKeywordBodyFromSchema(string keywordName, JsonSchema keywordBodySchema)
        {
            JsonSchema actualSchema = keywordBodySchema;
            if (!string.IsNullOrEmpty(keywordBodySchema.Ref))
            {
                TryGetDefinition(keywordBodySchema.Ref, out actualSchema);
            }


            switch (actualSchema.JsonType)
            {
                case "string":
                    var stringKeyword = new StringKeyword(keywordName);
                    if (!string.IsNullOrEmpty(actualSchema.Pattern))
                    {
                        stringKeyword.Pattern = actualSchema.Pattern;
                    }
                    return stringKeyword.AddProperties(actualSchema, v => v);

                case "boolean":
                    return new BooleanKeyword(keywordName);

                case "array":
                    return new ArrayKeyword(keywordName)
                    {
                        ElementSchema = GetKeywordBodyFromSchema(keywordName: null, actualSchema.Items),
                    };

                case "integer":
                    return new IntegerKeyword(keywordName).AddProperties(actualSchema, int.Parse);

                case "object":
                    var bodyKeyword = new BodyKeyword(keywordName);
                    if (actualSchema.Properties != null && actualSchema.Properties.Count > 0)
                    {
                        foreach (KeyValuePair<string, JsonSchema> subSchema in actualSchema.Properties)
                        {
                            bodyKeyword.BlockKeywords[subSchema.Key] = GetKeywordFromProperty(subSchema.Key, subSchema.Value);
                        }
                    }
                    return bodyKeyword;

                default:
                    Console.Error.WriteLine($"Unknown schema type: {actualSchema.JsonType}");
                    return null;
            }
        }

        private bool TryGetProperties(JsonSchema resourceJsonSchema, out JsonSchema propertiesSchema)
        {
            if (!resourceJsonSchema.Properties.TryGetValue("properties", out propertiesSchema))
            {
                propertiesSchema = null;
                return false;
            }

            if (!string.IsNullOrEmpty(propertiesSchema.Ref))
            {
                return TryGetDefinition(propertiesSchema.Ref, out propertiesSchema);
            }

            propertiesSchema = null;
            return false;
        }

        private bool TryGetDefinition(string definitionPath, out JsonSchema definitionSchema)
        {
            string definitionName = definitionPath.Substring("#/definitions/".Length);
            return _resourceSchema.Definitions.TryGetValue(definitionName, out definitionSchema);
        }
    }

    public class DslSchemaBuilder
    {
        private readonly CodeModel _codeModel;

        private readonly Dictionary<string, Dictionary<string, ResourceProviderBuilder>> _resourceProviders;

        public DslSchemaBuilder(CodeModel codeModel)
        {
            _codeModel = codeModel;
            _resourceProviders = new Dictionary<string, Dictionary<string, ResourceProviderBuilder>>();
        }

        public DslSchemaBuilder AddResourceProvider(
            string providerName,
            string apiVersion,
            ResourceSchema resourceSchema)
        {
            if (!_resourceProviders.TryGetValue(providerName, out Dictionary<string, ResourceProviderBuilder> providerVersions))
            {
                providerVersions = new Dictionary<string, ResourceProviderBuilder>();
                _resourceProviders[providerName] = providerVersions;
            }

            if (!providerVersions.TryGetValue(apiVersion, out ResourceProviderBuilder providerBuilder))
            {
                providerBuilder = new ResourceProviderBuilder(_codeModel, resourceSchema, providerName, apiVersion);
                providerVersions[apiVersion] = providerBuilder;
            }

            foreach (KeyValuePair<ResourceDescriptor, JsonSchema> resourceDefinition in resourceSchema.ResourceDefinitions)
            {
                providerBuilder.AddResource(resourceDefinition.Key.UnqualifiedType, resourceDefinition.Value);
            }

            return this;
        }

        public IEnumerable<(string, string, ResourceProviderBuilder)> GetProviders()
        {
            foreach (KeyValuePair<string, Dictionary<string, ResourceProviderBuilder>> providerKind in _resourceProviders)
            {
                foreach (KeyValuePair<string, ResourceProviderBuilder> providerVersion in providerKind.Value)
                {
                    yield return (providerKind.Key, providerVersion.Key, providerVersion.Value);
                }
            }
        }
    }
}
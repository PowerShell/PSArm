
// Copyright (c) Microsoft Corporation.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using PSArm.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Threading.Tasks;

namespace PSArm.Serialization
{
    public class ArmParser
    {
        private static readonly ArmStringLiteral s_defaultVersion = new ArmStringLiteral("1.0.0.0");

        private static readonly ArmStringLiteral s_defaultSchema = new ArmStringLiteral("https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#");

        private readonly ArmExpressionParser _armExpressionParser;

        public ArmParser()
        {
            _armExpressionParser = new ArmExpressionParser();
        }

        public ArmTemplate ParseString(string templateName, string str)
        {
            using (var reader = new StringReader(str))
            {
                return ParseStream(templateName, reader);
            }
        }

        public async Task<ArmTemplate> ParseUriAsync(Uri uri)
        {
            string templateName = null;
            try
            {
                templateName = Path.GetFileNameWithoutExtension(uri.LocalPath);
            }
            catch
            {
                // templateName will be null if we fail to parse the URI somehow
            }

            using (var webClient = new WebClient())
            using (Stream stream = await webClient.OpenReadTaskAsync(uri).ConfigureAwait(false))
            using (var reader = new StreamReader(stream))
            {
                return await ParseStreamAsync(templateName, reader).ConfigureAwait(false);
            }
        }

        public ArmTemplate ParseUri(Uri uri)
        {
            string templateName = null;
            try
            {
                templateName = Path.GetFileNameWithoutExtension(uri.LocalPath);
            }
            catch
            {
                // templateName will be null if we fail to parse the URI somehow
            }

            using (var webClient = new WebClient())
            using (Stream stream = webClient.OpenRead(uri))
            using (var reader = new StreamReader(stream))
            {
                return ParseStream(templateName, reader);
            }
        }

        public async Task<ArmTemplate> ParseFileAsync(string path)
        {
            using (StreamReader file = File.OpenText(path))
            {
                return await ParseStreamAsync(Path.GetFileNameWithoutExtension(path), file).ConfigureAwait(false);
            }
        }

        public ArmTemplate ParseFile(string path)
        {
            using (StreamReader file = File.OpenText(path))
            {
                return ParseStream(Path.GetFileNameWithoutExtension(path), file);
            }
        }

        public ArmTemplate ParseStream(string templateName, TextReader reader)
        {
            using (var jsonReader = new JsonTextReader(reader))
            {
                return ParseJObject(templateName, (JObject)JToken.ReadFrom(jsonReader));
            }
        }

        public async Task<ArmTemplate> ParseStreamAsync(string templateName, TextReader reader)
        {
            using (var jsonReader = new JsonTextReader(reader))
            {
                return ParseJObject(templateName, (JObject)(await JToken.ReadFromAsync(jsonReader).ConfigureAwait(false)));
            }
        }

        public ArmTemplate ParseJObject(string templateName, JObject templateObject)
        {
            var template = new ArmTemplate(templateName);

            if (templateObject.TryGetValue("$schema", out JToken schemaValue))
            {
                template.Schema = new ArmStringLiteral(CoerceJTokenToValue<string>(schemaValue));
            }
            else
            {
                template.Schema = s_defaultSchema;
            }

            if (templateObject.TryGetValue("contentVersion", out JToken contentVersionValue))
            {
                template.ContentVersion = new ArmStringLiteral(CoerceJTokenToValue<string>(contentVersionValue));
            }
            else
            {
                template.ContentVersion = s_defaultVersion;
            }

            if (templateObject.TryGetValue("parameters", out JToken parametersValue))
            {
                template.Parameters = ReadSubobject((JObject)parametersValue, new ArmObject<ArmParameter>(), ReadParameter);
            }

            if (templateObject.TryGetValue("variables", out JToken variablesValue))
            {
                template.Variables = ReadSubobject((JObject)variablesValue, new ArmObject<ArmVariable>(), ReadVariable);
            }

            if (templateObject.TryGetValue("resources", out JToken resourcesValue))
            {
                template.Resources = ReadArray((JArray)resourcesValue, new ArmArray<ArmResource>(), ReadResource);
            }

            if (templateObject.TryGetValue("outputs", out JToken outputsValue))
            {
                template.Outputs = ReadSubobject((JObject)outputsValue, new ArmObject<ArmOutput>(), ReadOutput);
            }

            return template;
        }

        private TObject ReadSubobject<TObject, TValue>(JObject jObj, TObject armObj, Func<string, JToken, TValue> convert) where TObject : ArmObject where TValue : ArmElement
        {
            foreach (KeyValuePair<string, JToken> entry in jObj)
            {
                var key = new ArmStringLiteral(entry.Key);
                armObj[key] = convert(entry.Key, entry.Value);
            }
            return armObj;
        }

        private ArmArray ReadArray(JArray array, Func<JToken, ArmElement> convert)
            => ReadArray(array, new ArmArray(), convert);

        private TArray ReadArray<TArray, TElement>(JArray array, TArray armArray, Func<JToken, TElement> convert) where TArray : ArmArray where TElement : ArmElement
        {
            for (int i = 0; i < array.Count; i++)
            {
                armArray.Add(convert(array[i]));
            }
            return armArray;
        }

        private ArmParameter ReadParameter(string parameterName, JToken parameterToken)
        {
            var parameterObject = (JObject)parameterToken;

            string type = parameterObject["type"].Value<string>().ToLower();

            switch (type)
            {
                case "string":
                    return ReadTypedParameter<string>(parameterName, parameterObject);
                case "securestring":
                    return ReadTypedParameter<SecureString>(parameterName, parameterObject);
                case "int":
                    return ReadTypedParameter<int>(parameterName, parameterObject);
                case "bool":
                    return ReadTypedParameter<bool>(parameterName, parameterObject);
                case "object":
                    return ReadTypedParameter<object>(parameterName, parameterObject);
                case "secureobject":
                    return ReadTypedParameter<SecureObject>(parameterName, parameterObject);
                case "array":
                    return ReadTypedParameter<Array>(parameterName, parameterObject);
                default:
                    throw new ArgumentException($"Unsupported type '{type}' on ARM parameter '{parameterName}'");
            }
        }

        private ArmParameter ReadTypedParameter<T>(string parameterName, JObject parameterObject)
        {
            var parameter = new ArmParameter<T>(new ArmStringLiteral(parameterName));

            if (parameterObject.TryGetValue("defaultValue", out JToken defaultValue))
            {
                parameter.DefaultValue = ReadValue(defaultValue);
            }

            if (parameterObject.TryGetValue("allowedValues", out JToken allowedValues))
            {
                parameter.AllowedValues = ReadArmArray((JArray)allowedValues);
            }

            return parameter;
        }

        private ArmVariable ReadVariable(string variableName, JToken variableObject)
        {
            return new ArmVariable(new ArmStringLiteral(variableName), ReadValue(variableObject));
        }

        private ArmResource ReadResource(JToken resourceToken)
        {
            var resourceObject = (JObject)resourceToken;

            var resource = new ArmResource
            {
                Name = ReadArmExpression(resourceObject["name"]),
                ApiVersion = new ArmStringLiteral(CoerceJTokenToValue<string>(resourceToken["apiVersion"])),
                Type = new ArmStringLiteral(CoerceJTokenToValue<string>(resourceToken["type"])),
            };

            if (resourceObject.TryGetValue("location", out JToken locationObject))
            {
                resource[ArmTemplateKeys.Location] = (ArmElement)ReadArmExpression(locationObject);
            }

            if (resourceObject.TryGetValue("dependsOn", out JToken dependsOnArray))
            {
                resource.DependsOn = ReadArray((JArray)dependsOnArray, ReadArmExpressionAsArmElement);
            }

            if (resourceObject.TryGetValue("kind", out JToken kindToken))
            {
                resource[ArmTemplateKeys.Kind] = (ArmElement)ReadArmExpression(kindToken);
            }

            if (resourceObject.TryGetValue("sku", out JToken skuObject))
            {
                resource.Sku = ReadSku((JObject)skuObject);
            }

            if (resourceObject.TryGetValue("properties", out JToken propertiesObject))
            {
                resource.Properties = ReadProperties((JObject)propertiesObject);
            }

            return resource;
        }

        private ArmSku ReadSku(JObject skuObject)
        {
            var sku = new ArmSku();

            if (skuObject.TryGetValue("name", out JToken nameValue))
            {
                sku.Name = ReadArmExpression(nameValue);
            }

            if (skuObject.TryGetValue("tier", out JToken tierValue))
            {
                sku.Tier = ReadArmExpression(tierValue);
            }

            if (skuObject.TryGetValue("size", out JToken sizeValue))
            {
                sku.Size = ReadArmExpression(sizeValue);
            }

            if (skuObject.TryGetValue("family", out JToken familyValue))
            {
                sku.Family = ReadArmExpression(familyValue);
            }

            if (skuObject.TryGetValue("capacity", out JToken capacityValue))
            {
                sku.Capacity = ReadArmExpression(capacityValue);
            }

            return sku;
        }

        private ArmObject<ArmObject> ReadProperties(JObject propertiesObject)
        {
            var properties = new ArmObject<ArmObject>();

            ReadPropertiesIntoDict(properties, propertiesObject);

            return properties;
        }

        private void ReadPropertiesIntoDict(ArmObject<ArmObject> armObject, JObject propertiesObject)
        {
            foreach (KeyValuePair<string, JToken> entry in propertiesObject)
            {
                switch (entry.Value)
                {
                    case JObject objectProperty:
                        armObject[new ArmStringLiteral(entry.Key)] = ReadArmObject(objectProperty);
                        continue;

                    case JArray arrayProperty:
                        armObject[new ArmStringLiteral(entry.Key)] = ReadArmArray(arrayProperty);
                        continue;

                    case JValue valueProperty:
                        armObject[new ArmStringLiteral(entry.Key)] = ReadArmValue(valueProperty);
                        continue;

                    default:
                        throw new ArgumentOutOfRangeException($"Unknown JSON token type '{entry.Value.GetType().FullName}' for property '{entry.Key}'");
                }
            }
        }

        private ArmOutput ReadOutput(string outputName, JToken outputToken)
        {
            var outputObject = (JObject)outputToken;

            var output = new ArmOutput(ReadArmExpression(outputName))
            {
                Value = ReadArmExpression(outputToken["value"]),
            };

            if (outputObject.TryGetValue("type", out JToken typeToken))
            {
                output.Type = ReadArmExpression(typeToken);
            }

            return output;
        }

        private ArmElement ReadValue(JToken jToken)
        {
            switch (jToken)
            {
                case JValue value:
                    return ReadArmValue(value);

                case JObject jObject:
                    return ReadArmObject(jObject);

                case JArray jArray:
                    return ReadArmArray(jArray);

                default:
                    throw new ArgumentOutOfRangeException($"Unknown type of argument '{jToken}': '{jToken.GetType().FullName}'");
            }
        }

        private ArmElement ReadArmValue(JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Null:
                    return ArmNullLiteral.Value;

                case JTokenType.Boolean:
                    return ArmBooleanLiteral.FromBool(value.Value<bool>());

                case JTokenType.Integer:
                    return new ArmIntegerLiteral(value.Value<long>());

                default:
                    return (ArmElement)ReadArmExpression(value);
            }
        }

        private ArmObject ReadArmObject(JObject jObject)
        {
            var armObject = new ArmObject();

            foreach (KeyValuePair<string, JToken> entry in jObject)
            {
                armObject[new ArmStringLiteral(entry.Key)] = ReadValue(entry.Value);
            }

            return armObject;
        }

        private ArmArray ReadArmArray(JArray jArray)
        {
            var armArray = new ArmArray();

            foreach (JToken item in jArray)
            {
                armArray.Add(ReadValue(item));
            }

            return armArray;
        }

        private ArmElement ReadArmExpressionAsArmElement(JToken jToken) => (ArmElement)ReadArmExpression(jToken);

        private IArmString ReadArmExpression(string exprStr)
        {
            return _armExpressionParser.ParseExpression(exprStr);
        }

        private IArmString ReadArmExpression(JValue jValue) => ReadArmExpression(jValue.Value<string>());

        private IArmString ReadArmExpression(JToken jToken) => ReadArmExpression((JValue)jToken);

        private T CoerceJTokenToValue<T>(JToken jToken)
        {
            return ((JValue)jToken).Value<T>();
        }
    }
}
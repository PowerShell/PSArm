using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSArm.Templates;
using PSArm.Templates.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PSArm.Serialization
{
    public class ArmParser
    {
        private static readonly ArmStringValue s_defaultVersion = new ArmStringValue("1.0.0.0");

        private static readonly ArmStringValue s_defaultSchema = new ArmStringValue("https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#");

        private readonly ArmExpressionParser _armExpressionParser;

        public ArmParser()
        {
            _armExpressionParser = new ArmExpressionParser();
        }

        public ArmTemplate ParseString(string str)
        {
            using (var reader = new StringReader(str))
            {
                return ParseStream(reader);
            }
        }

        public async Task<ArmTemplate> ParseUriAsync(Uri uri)
        {
            using (var webClient = new WebClient())
            using (Stream stream = await webClient.OpenReadTaskAsync(uri))
            using (var reader = new StreamReader(stream))
            {
                return await ParseStreamAsync(reader);
            }
        }

        public ArmTemplate ParseUri(Uri uri)
        {
            using (var webClient = new WebClient())
            using (Stream stream = webClient.OpenRead(uri))
            using (var reader = new StreamReader(stream))
            {
                return ParseStream(reader);
            }
        }

        public async Task<ArmTemplate> ParseFileAsync(string path)
        {
            using (StreamReader file = File.OpenText(path))
            {
                return await ParseStreamAsync(file);
            }
        }

        public ArmTemplate ParseFile(string path)
        {
            using (StreamReader file = File.OpenText(path))
            {
                return ParseStream(file);
            }
        }

        public ArmTemplate ParseStream(TextReader reader)
        {
            using (var jsonReader = new JsonTextReader(reader))
            {
                return ParseJObject((JObject)JToken.ReadFrom(jsonReader));
            }
        }

        public async Task<ArmTemplate> ParseStreamAsync(TextReader reader)
        {
            using (var jsonReader = new JsonTextReader(reader))
            {
                return ParseJObject((JObject)(await JToken.ReadFromAsync(jsonReader)));
            }
        }

        public ArmTemplate ParseJObject(JObject templateObject)
        {
            var template = new ArmTemplate();

            if (templateObject.TryGetValue("$schema", out JToken schemaValue))
            {
                template.Schema = new ArmStringValue(((JValue)schemaValue).Value<string>());
            }
            else
            {
                template.Schema = s_defaultSchema;
            }

            if (templateObject.TryGetValue("contentVersion", out JToken contentVersionValue))
            {
                template.ContentVersion = new ArmStringValue(((JValue)contentVersionValue).Value<string>());
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
                var key = new ArmStringValue(entry.Key);
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
                case "securestring":
                    return ReadTypedParameter<string>(parameterName, parameterObject);
                case "int":
                    return ReadTypedParameter<long>(parameterName, parameterObject);
                case "bool":
                    return ReadTypedParameter<bool>(parameterName, parameterObject);
                case "object":
                case "secureobject":
                    return ReadTypedParameter<Hashtable>(parameterName, parameterObject);
                case "array":
                default:
                    throw new ArgumentException($"Unsupported type '{type}' on ARM parameter '{parameterName}'");
            }
        }

        private ArmParameter ReadTypedParameter<T>(string parameterName, JObject parameterObject)
        {
            var parameter = new ArmParameter(new ArmStringValue(parameterName));

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
            return new ArmVariable(new ArmStringValue(variableName), ReadValue(variableObject));
        }

        private ArmResource ReadResource(JToken resourceToken)
        {
            var resourceObject = (JObject)resourceToken;

            var resource = new ArmResource
            {
                Name = ReadArmExpression(resourceObject["name"]),
                ApiVersion = new ArmStringValue(CoerceJTokenToValue<string>(resourceToken["apiVersion"])),
                Type = new ArmStringValue(CoerceJTokenToValue<string>(resourceToken["type"])),
            };

            if (resourceObject.TryGetValue("location", out JToken locationObject))
            {
                resource.Location = ReadArmExpression(locationObject);
            }

            if (resourceObject.TryGetValue("dependsOn", out JToken dependsOnArray))
            {
                resource.DependsOn = ReadArray((JArray)dependsOnArray, ReadArmExpressionAsArmElement);
            }

            if (resourceObject.TryGetValue("kind", out JToken kindToken))
            {
                resource.Kind = ReadArmExpression(kindToken);
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
                        armObject[new ArmStringValue(entry.Key)] = ReadArmObject(objectProperty);
                        continue;

                    case JArray arrayProperty:
                        armObject[new ArmStringValue(entry.Key)] = ReadArmArray(arrayProperty);
                        continue;

                    case JValue valueProperty:
                        armObject[new ArmStringValue(entry.Key)] = ReadArmValue(valueProperty);
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
                    return ArmNullValue.Value;

                case JTokenType.Boolean:
                    return ArmBooleanValue.FromBool(value.Value<bool>());

                case JTokenType.Integer:
                    return new ArmIntegerValue(value.Value<long>());

                default:
                    return (ArmElement)ReadArmExpression(value);
            }
        }

        private ArmObject ReadArmObject(JObject jObject)
        {
            var armObject = new ArmObject();

            foreach (KeyValuePair<string, JToken> entry in jObject)
            {
                armObject[new ArmStringValue(entry.Key)] = ReadValue(entry.Value);
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
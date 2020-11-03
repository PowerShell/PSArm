using Microsoft.PowerShell.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSArm.ArmBuilding;
using PSArm.Expression;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Resources;
using System.Threading.Tasks;

namespace PSArm
{
    public class ArmParser
    {
        private static readonly Version s_defaultVersion = new Version(1, 0, 0, 0);

        private const string DefaultSchema = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#";

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

            template.Schema = templateObject["$schema"].Value<string>() ?? DefaultSchema;

            if (templateObject.TryGetValue("contentVersion", out JToken contentVersionValue))
            {
                template.ContentVersion = Version.Parse(((JValue)contentVersionValue).Value<string>());
            }
            else
            {
                template.ContentVersion = s_defaultVersion;
            }

            if (templateObject.TryGetValue("parameters", out JToken parametersValue))
            {
                template.Parameters = ReadListFromObject((JObject)parametersValue, ReadParameter);
            }

            if (templateObject.TryGetValue("variables", out JToken variablesValue))
            {
                template.Variables = ReadListFromObject((JObject)variablesValue, ReadVariable);
            }

            if (templateObject.TryGetValue("resources", out JToken resourcesValue))
            {
                template.Resources = ReadList((JArray)resourcesValue, ReadResource);
            }

            if (templateObject.TryGetValue("outputs", out JToken outputsValue))
            {
                template.Outputs = ReadListFromObject((JObject)outputsValue, ReadOutput);
            }

            return template;
        }

        private List<T> ReadListFromObject<T>(JObject jObj, Func<string, JToken, T> convert)
        {
            var list = new List<T>(jObj.Count);
            foreach (KeyValuePair<string, JToken> value in jObj)
            {
                list.Add(convert(value.Key, value.Value));
            }
            return list;
        }

        private List<T> ReadList<T>(JArray array, Func<JToken, T> convert)
        {
            var list = new List<T>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                list.Add(convert(array[i]));
            }
            return list;
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

        private ArmParameter<T> ReadTypedParameter<T>(string parameterName, JObject parameterObject)
        {
            var parameter = new ArmParameter<T>(parameterName);

            if (parameterObject.TryGetValue("defaultValue", out JToken defaultValue))
            {
                parameter.DefaultValue = ReadValue(defaultValue);
            }

            if (parameterObject.TryGetValue("allowedValues", out JToken allowedValues))
            {
                parameter.AllowedValues = ((JArray)allowedValues).Select(v => ReadValue(v)).ToList();
            }

            return parameter;
        }

        private ArmVariable ReadVariable(string variableName, JToken variableObject)
        {
            return new ArmVariable(variableName, ReadValue(variableObject));
        }

        private ArmResource ReadResource(JToken resourceToken)
        {
            var resourceObject = (JObject)resourceToken;

            var resource = new ArmResource
            {
                Name = ReadArmExpression(resourceObject["name"]),
                ApiVersion = CoerceJTokenToValue<string>(resourceToken["apiVersion"]),
                Type = CoerceJTokenToValue<string>(resourceToken["type"]),
            };

            if (resourceObject.TryGetValue("location", out JToken locationObject))
            {
                resource.Location = ReadArmExpression(locationObject);
            }

            if (resourceObject.TryGetValue("dependsOn", out JToken dependsOnArray))
            {
                resource.DependsOn = ReadList((JArray)dependsOnArray, ReadArmExpression);
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

        private Dictionary<string, ArmPropertyInstance> ReadProperties(JObject propertiesObject)
        {
            var properties = new Dictionary<string, ArmPropertyInstance>();

            ReadPropertiesIntoDict(properties, propertiesObject);

            return properties;
        }

        private void ReadPropertiesIntoDict(Dictionary<string, ArmPropertyInstance> propertyDict, JObject propertiesObject)
        {
            foreach (KeyValuePair<string, JToken> entry in propertiesObject)
            {
                switch (entry.Value)
                {
                    case JObject objectProperty:
                        propertyDict[entry.Key] = ReadObjectProperty(entry.Key, objectProperty);
                        continue;

                    case JArray arrayProperty:
                        propertyDict[entry.Key] = ReadArrayProperty(entry.Key, arrayProperty);
                        continue;

                    case JValue valueProperty:
                        propertyDict[entry.Key] = ReadValueProperty(entry.Key, valueProperty);
                        continue;

                    default:
                        throw new ArgumentOutOfRangeException($"Unknown JSON token type '{entry.Value.GetType().FullName}' for property '{entry.Key}'");
                }
            }
        }

        private ArmPropertyObject ReadObjectProperty(string name, JObject propertyObject)
        {
            var property = new ArmPropertyObject(name);

            foreach (KeyValuePair<string, JToken> entry in propertyObject)
            {
                if (entry.Key.Equals("properties", StringComparison.Ordinal))
                {
                    ReadPropertiesIntoDict(property.Properties, (JObject)entry.Value);
                    continue;
                }

                property.Parameters[entry.Key] = ReadValue(entry.Value);
            }

            return property;
        }

        private ArmPropertyValue ReadValueProperty(string name, JValue propertyValue)
        {
            return new ArmPropertyValue(name, ReadValue(propertyValue));
        }

        private ArmPropertyArray ReadArrayProperty(string name, JArray propertyArray)
        {
            var array = new ArmPropertyArray(name);

            foreach (JToken item in propertyArray)
            {
                array.Items.Add(ReadArrayPropertyItem(name, item));
            }

            return array;
        }

        private ArmPropertyArrayItem ReadArrayPropertyItem(string name, JToken propertyItem)
        {
            var item = new ArmPropertyArrayItem(name);

            foreach (KeyValuePair<string, JToken> entry in (JObject)propertyItem)
            {
                if (entry.Key.Equals("properties", StringComparison.Ordinal))
                {
                    ReadPropertiesIntoDict(item.Properties, (JObject)entry.Value);
                    continue;
                }

                item.Parameters[entry.Key] = ReadValue(entry.Value);
            }

            return item;
        }

        private ArmOutput ReadOutput(string outputName, JToken outputToken)
        {
            var outputObject = (JObject)outputToken;

            var output = new ArmOutput
            {
                Name = ReadArmExpression(outputName),
                Value = ReadArmExpression(outputToken["value"]),
            };

            if (outputObject.TryGetValue("type", out JToken typeToken))
            {
                output.Type = ReadArmExpression(typeToken);
            }

            return output;
        }

        private IArmValue ReadValue(JToken jToken)
        {
            switch (jToken)
            {
                case JValue value:
                    return ReadArmExpression(value);

                case JObject jObject:
                    return ReadArmObject(jObject);

                case JArray jArray:
                    return ReadArmArray(jArray);

                default:
                    throw new ArgumentOutOfRangeException($"Unknown type of argument '{jToken}': '{jToken.GetType().FullName}'");
            }
        }

        private ArmObject ReadArmObject(JObject jObject)
        {
            var armObject = new ArmObject();

            foreach (KeyValuePair<string, JToken> entry in jObject)
            {
                armObject[entry.Key] = ReadValue(entry.Value);
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

        private IArmExpression ReadArmExpression(string exprStr)
        {
            return (IArmExpression)ReadValue(exprStr);
        }

        private IArmExpression ReadArmExpression(JToken jToken)
        {
            return _armExpressionParser.ParseExpression(CoerceJTokenToValue<string>(jToken));
        }

        private T CoerceJTokenToValue<T>(JToken jToken)
        {
            return ((JValue)jToken).Value<T>();
        }
    }
}
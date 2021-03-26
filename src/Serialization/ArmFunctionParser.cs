
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace PSArm.Serialization
{
    public class ArmFunctionParser
    {
        public Dictionary<string, ArmBuiltinFunction> ParseFromUri(Uri uri)
        {
            using (var webClient = new WebClient())
            using (Stream stream = webClient.OpenRead(uri))
            using (var reader = new StreamReader(stream))
            {
                return ParseFromTextReader(reader);
            }
        }

        public Dictionary<string, ArmBuiltinFunction> ParseFromString(string input)
        {
            using (var stringReader = new StringReader(input))
            {
                return ParseFromTextReader(stringReader);
            }
        }

        public Dictionary<string, ArmBuiltinFunction> ParseFromFile(string path)
        {
            using (var streamReader = new StreamReader(path))
            {
                return ParseFromTextReader(streamReader);
            }
        }

        public Dictionary<string, ArmBuiltinFunction> ParseFromTextReader(TextReader reader)
        {
            using (var jsonReader = new JsonTextReader(reader))
            {
                return ParseFromJToken(JToken.ReadFrom(jsonReader));
            }
        }

        public Dictionary<string, ArmBuiltinFunction> ParseFromJToken(JToken functionSpecDocument)
        {
            var functions = new Dictionary<string, ArmBuiltinFunction>(StringComparer.OrdinalIgnoreCase);

            var signatureArray = (JArray)((JObject)functionSpecDocument)["functionSignatures"];
            foreach (JObject entry in signatureArray)
            {
                ArmBuiltinFunction function = ConvertFromJObject(entry);
                functions[function.Name] = function;
            }

            return functions;
        }

        private ArmBuiltinFunction ConvertFromJObject(JObject functionEntry)
        {
            string name = functionEntry["name"].Value<string>();
            string description = functionEntry["description"].Value<string>();
            int minArgs = functionEntry["minimumArguments"].Value<int>();

            int? maxArgs = null;
            if (functionEntry.TryGetValue("maximumArguments", out JToken maxArgsToken)
                && maxArgsToken.Type != JTokenType.Null)
            {
                maxArgs = maxArgsToken.Value<int>();
            }

            List<string> returnValueMembers = null;
            if (functionEntry.TryGetValue("returnValueMembers", out JToken rvmToken)
                && rvmToken.Type != JTokenType.Null)
            {
                returnValueMembers = new List<string>();
                foreach (JObject item in (JArray)rvmToken)
                {
                    returnValueMembers.Add(item["name"].Value<string>());
                }
            }

            return new ArmBuiltinFunction(name, description, minArgs, maxArgs, returnValueMembers?.ToArray());
        }
    }
}

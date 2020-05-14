using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSArm.Schema
{
    public class DslSchemaConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObj = (JObject)JObject.ReadFrom(reader);
            return ReadSchema(jObj);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(DslSchemaItem).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => false;

        private DslSchemaItem ReadSchema(JObject jObj)
        {
            if (!Enum.TryParse(jObj["kind"].Value<string>(), ignoreCase: true, out DslSchemaKind kind))
            {
                throw new JsonSerializationException();
            }

            switch (kind)
            {
                case DslSchemaKind.Array:
                    return ReadArraySchema(jObj);

                case DslSchemaKind.Block:
                    return ReadBlockSchema(jObj);

                case DslSchemaKind.Command:
                    return ReadCommandSchema(jObj);

                case DslSchemaKind.BodyCommand:
                    return ReadBodyCommandSchema(jObj); 

                default:
                    throw new JsonSerializationException();
            }
        }

        private DslArraySchema ReadArraySchema(JObject jObj)
        {
            Dictionary<string, DslSchemaItem> body = null;
            if (jObj.TryGetValue("body", StringComparison.Ordinal, out JToken value))
            {
                body = new Dictionary<string, DslSchemaItem>();
                var bodyValue = (JObject)value;
                foreach (KeyValuePair<string, JToken> entry in bodyValue)
                {
                    body[entry.Key] = ReadSchema((JObject)entry.Value);
                }
            }

            return new DslArraySchema
            {
                Body = body,
                Parameters = ReadParameters(jObj),
            };
        }

        private DslBlockSchema ReadBlockSchema(JObject jObj)
        {
            var dict = new Dictionary<string, DslSchemaItem>();
            foreach (KeyValuePair<string, JToken> entry in (JObject)jObj["body"])
            {
                dict[entry.Key] = ReadSchema((JObject)entry.Value);
            }

            return new DslBlockSchema
            {
                Parameters = ReadParameters(jObj),
                Body = dict,
            };
        }

        private DslCommandSchema ReadCommandSchema(JObject jObj)
        {
            return new DslCommandSchema
            {
                Parameters = ReadParameters(jObj),
            };
        }

        private DslBodyCommandSchema ReadBodyCommandSchema(JObject jObj)
        {
            return new DslBodyCommandSchema
            {
                Parameters = ReadParameters(jObj),
            };
        }

        private List<DslParameter> ReadParameters(JObject jObj)
        {
            if (!jObj.TryGetValue("parameters", StringComparison.Ordinal, out JToken value))
            {
                return null;
            }

            return value.ToObject<List<DslParameter>>();
        }
    }
}

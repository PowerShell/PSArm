using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace RobImpl.ArmSchema
{
    public abstract class Union<T1, T2>
    {
        public abstract T Match<T>(Func<T1, T> f1, Func<T2, T> f2);

        private Union()
        {
        }

        public sealed class Case1 : Union<T1, T2>
        {
            public Case1(T1 value)
            {
                Value = value;
            }

            public T1 Value { get; }

            public override T Match<T>(Func<T1, T> f1, Func<T2, T> f2)
            {
                return f1(Value);
            }
        }

        public sealed class Case2 : Union<T1, T2>
        {
            public Case2(T2 value)
            {
                Value = value;
            }

            public T2 Value { get; }

            public override T Match<T>(Func<T1, T> f1, Func<T2, T> f2)
            {
                return f2(Value);
            }
        }
    }

    public class ArmSchemaBuildingVisitor
    {
        public ArmJsonSchema CreateFromHttpUri(Uri uri)
        {
            return CreateFromJsonDocument(JsonDocument.FromWebUri(uri));
        }

        public ArmJsonSchema CreateFromJsonDocument(JsonDocument document)
        {
            return CreateSchema((JsonObject)document.Root);
        }

        public ArmJsonSchema CreateSchema(JsonObject jObj)
        {
            JsonSchemaType[] schemaTypes = GetSchemaTypeFromObject(jObj);

            ArmConcreteSchema schema = null;
            if (schemaTypes != null && schemaTypes.Length == 1)
            {
                switch (schemaTypes[0])
                {
                    case JsonSchemaType.AllOf:
                        return CreateAllOf(CoerceJson<JsonArray>(jObj.Fields["allOf"]));

                    case JsonSchemaType.AnyOf:
                        return CreateAnyOf(CoerceJson<JsonArray>(jObj.Fields["anyOf"]));

                    case JsonSchemaType.OneOf:
                        return CreateOneOf(CoerceJson<JsonArray>(jObj.Fields["oneOf"]));

                    case JsonSchemaType.Not:
                        return CreateNot(CoerceJson<JsonObject>(jObj.Fields["not"]));

                    case JsonSchemaType.Array:
                        schema = CreateArray(jObj);
                        break;

                    case JsonSchemaType.Boolean:
                        schema = CreateBoolean(jObj);
                        break;

                    case JsonSchemaType.Null:
                        schema = CreateNull(jObj);
                        break;

                    case JsonSchemaType.Number:
                        schema = CreateNumeric<ArmNumberSchema, JsonNumber, double>(jObj);
                        break;

                    case JsonSchemaType.Integer:
                        schema = CreateNumeric<ArmIntegerSchema, JsonInteger, long>(jObj);
                        break;

                    case JsonSchemaType.Object:
                        schema = CreateObject(jObj);
                        break;

                    case JsonSchemaType.String:
                        schema = CreateString(jObj);
                        break;
                }
            }

            if (schema == null)
            {
                throw new NotImplementedException($"Schema of types '{string.Join(",", schemaTypes)}' not supported");
            }

            SetCommonFields(jObj, schema);

            return schema;
        }

        private ArmAllOfCombinator CreateAllOf(JsonArray jArr)
        {
            return new ArmAllOfCombinator
            {
                AllOf = CreateSchemaArray(jArr.Items),
            };
        }

        private ArmAnyOfCombinator CreateAnyOf(JsonArray jArr)
        {
            return new ArmAnyOfCombinator
            {
                AnyOf = CreateSchemaArray(jArr.Items),
            };
        }

        private ArmOneOfCombinator CreateOneOf(JsonArray jArr)
        {
            return new ArmOneOfCombinator
            {
                OneOf = CreateSchemaArray(jArr.Items),
            };
        }

        private ArmNotCombinator CreateNot(JsonObject jObj)
        {
            return new ArmNotCombinator
            {
                Not = CreateSchema(jObj),
            };
        }

        private ArmArraySchema CreateArray(JsonObject jObj)
        {
            int? length = null;
            if (jObj.Fields.TryGetValue("length", out JsonItem lengthField))
            {
                length = (int)CoerceJson<JsonNumber>(lengthField).Value;
            }

            bool? unique = null;
            if (jObj.Fields.TryGetValue("uniqueItems", out JsonItem uniqueItemsField))
            {
                unique = CoerceJson<JsonBoolean>(uniqueItemsField).Value;
            }

            if (!jObj.Fields.TryGetValue("items", out JsonItem itemsField))
            {
                return new ArmListSchema
                {
                    Length = length,
                    UniqueItems = unique,
                };
            }

            if (TryCoerceJson(itemsField, out JsonObject itemsSchemaObject))
            {
                return new ArmListSchema
                {
                    Length = length,
                    UniqueItems = unique,
                    Items = CreateSchema(itemsSchemaObject),
                };
            }

            Union<bool, ArmJsonSchema> additionalItems = null;
            if (jObj.Fields.TryGetValue("additionalItems", out JsonItem additionalItemsField))
            {
                if (TryCoerceJson(additionalItemsField, out JsonBoolean additionalItemsBool))
                {
                    additionalItems = new Union<bool, ArmJsonSchema>.Case1(additionalItemsBool.Value);
                }
                else
                {
                    additionalItems = new Union<bool, ArmJsonSchema>.Case2(CreateSchema(CoerceJson<JsonObject>(additionalItemsField)));
                }
            }

            return new ArmTupleSchema
            {
                Length = length,
                UniqueItems = unique,
                AdditionalItems = additionalItems,
                Items = CreateSchemaArray(CoerceJson<JsonArray>(itemsField).Items),
            };
        }

        private ArmBooleanSchema CreateBoolean(JsonObject jObj)
        {
            return new ArmBooleanSchema();
        }

        private ArmNullSchema CreateNull(JsonObject jObj)
        {
            return new ArmNullSchema();
        }

        private TSchema CreateNumeric<TSchema, TJsonNumeric, TNumeric>(JsonObject jObj)
            where TSchema : ArmNumericSchema<TNumeric>, new()
            where TJsonNumeric : JsonValue<TNumeric>
            where TNumeric : struct
        {
            TNumeric? multipleOf = null;
            if (jObj.Fields.TryGetValue("multipleOf", out JsonItem multipleOfField))
            {
                multipleOf = CoerceJson<TJsonNumeric>(multipleOfField).Value;
            }

            TNumeric? minimum = null;
            if (jObj.Fields.TryGetValue("minimum", out JsonItem minimumField))
            {
                minimum = CoerceJson<TJsonNumeric>(minimumField).Value;
            }

            TNumeric? maximum = null;
            if (jObj.Fields.TryGetValue("maximum", out JsonItem maximumField))
            {
                maximum = CoerceJson<TJsonNumeric>(maximumField).Value;
            }

            bool exclusiveMinimum = false;
            if (jObj.Fields.TryGetValue("exclusiveMaximum", out JsonItem exclusiveMinimumField))
            {
                exclusiveMinimum = CoerceJson<JsonBoolean>(exclusiveMinimumField).Value;
            }

            bool exclusiveMaximum = false;
            if (jObj.Fields.TryGetValue("exclusiveMaximum", out JsonItem exclusiveMaximumField))
            {
                exclusiveMaximum = CoerceJson<JsonBoolean>(exclusiveMaximumField).Value;
            }

            return new TSchema
            {
                MultipleOf = multipleOf,
                Minimum = minimum,
                Maximum = maximum,
                ExclusiveMinimum = exclusiveMinimum,
                ExclusiveMaximum = exclusiveMaximum,
            };
        }

        private ArmStringSchema CreateString(JsonObject jObj)
        {
            long? minLength = null;
            if (jObj.Fields.TryGetValue("minLength", out JsonItem minLengthField))
            {
                minLength = CoerceJson<JsonInteger>(minLengthField).Value;
            }

            long? maxLength = null;
            if (jObj.Fields.TryGetValue("maxLength", out JsonItem maxLengthField))
            {
                maxLength = CoerceJson<JsonInteger>(maxLengthField).Value;
            }

            string pattern = null;
            if (jObj.Fields.TryGetValue("pattern", out JsonItem patternField))
            {
                pattern = CoerceJson<JsonString>(patternField).Value;
            }

            StringFormat? format = null;
            if (jObj.Fields.TryGetValue("format", out JsonItem formatField))
            {
                string formatStr = CoerceJson<JsonString>(formatField).Value;

                format = GetFormatFromString(formatStr);
            }

            return new ArmStringSchema
            {
                MinLength = minLength,
                MaxLength = maxLength,
                Pattern = pattern,
                Format = format,
            };
        }

        private ArmObjectSchema CreateObject(JsonObject jObj)
        {
            Dictionary<string, ArmJsonSchema> properties = null;
            if (jObj.Fields.TryGetValue("properties", out JsonItem propertiesField))
            {
                JsonObject propertiesObject = CoerceJson<JsonObject>(propertiesField);
                properties = new Dictionary<string, ArmJsonSchema>();
                foreach (KeyValuePair<string, JsonItem> entry in propertiesObject.Fields)
                {
                    properties[entry.Key] = CreateSchema(CoerceJson<JsonObject>(entry.Value));
                }
            }

            Union<bool, ArmJsonSchema> additionalProperties = null;
            if (jObj.Fields.TryGetValue("additionalProperties", out JsonItem additionalPropertiesField))
            {
                if (TryCoerceJson(additionalPropertiesField, out JsonBoolean jsonBoolean))
                {
                    additionalProperties = new Union<bool, ArmJsonSchema>.Case1(jsonBoolean.Value);
                }
                else
                {
                    additionalProperties = new Union<bool, ArmJsonSchema>.Case2(CreateSchema(CoerceJson<JsonObject>(additionalPropertiesField)));
                }
            }

            string[] required = null;
            if (jObj.Fields.TryGetValue("required", out JsonItem requiredField))
            {
                JsonItem[] requiredJsonItems = CoerceJson<JsonArray>(requiredField).Items;
                required = new string[requiredJsonItems.Length];
                for (int i = 0; i < required.Length; i++)
                {
                    required[i] = CoerceJson<JsonString>(requiredJsonItems[i]).Value;
                }
            }

            long? minProperties = null;
            if (jObj.Fields.TryGetValue("minProperties", out JsonItem minPropertiesField))
            {
                minProperties = CoerceJson<JsonInteger>(minPropertiesField).Value;
            }

            long? maxProperties = null;
            if (jObj.Fields.TryGetValue("maxProperties", out JsonItem maxPropertiesField))
            {
                maxProperties = CoerceJson<JsonInteger>(maxPropertiesField).Value;
            }

            return new ArmObjectSchema
            {
                AdditionalProperties = additionalProperties,
                MaxProperties = maxProperties,
                MinProperties = minProperties,
                Properties = properties,
                Required = required,
            };
        }

        private ArmJsonSchema[] CreateSchemaArray(JsonItem[] items)
        {
            var schemas = new ArmJsonSchema[items.Length];
            for (int i = 0; i < schemas.Length; i++)
            {
                schemas[i] = CreateSchema(CoerceJson<JsonObject>(items[i]));
            }
            return schemas;
        }

        private void SetCommonFields(JsonObject jObj, ArmConcreteSchema schema)
        {
            if (jObj.Fields.TryGetValue("$schema", out JsonItem schemaField))
            {
                schema.SchemaVersion = CoerceJson<JsonString>(schemaField).Value;
            }

            if (jObj.Fields.TryGetValue("title", out JsonItem titleField))
            {
                schema.Title = CoerceJson<JsonString>(titleField).Value;
            }

            if (jObj.Fields.TryGetValue("description", out JsonItem descriptionField))
            {
                schema.Description = CoerceJson<JsonString>(descriptionField).Value;
            }

            if (jObj.Fields.TryGetValue("enum", out JsonItem enumField))
            {
                JsonItem[] enumFields = CoerceJson<JsonArray>(enumField).Items;
                var enumVals = new object[enumFields.Length];
                for (int i = 0; i < enumVals.Length; i++)
                {
                    enumVals[i] = GetValueFromJsonValue(enumFields[i]);
                }
            }
        }

        private TJson CoerceJson<TJson>(JsonItem jItem) where TJson : JsonItem
        {
            switch (jItem)
            {
                case JsonPointer jPtr:
                    return CoerceJson<TJson>(jPtr.ResolvedItem);

                case TJson jType:
                    return jType;

                default:
                    throw new InvalidCastException($"Cannot convert type '{jItem.GetType()}' to type '{typeof(TJson)}'");
            }
        }

        private bool TryCoerceJson<TJson>(JsonItem jItem, out TJson jType) where TJson : JsonItem
        {
            switch (jItem)
            {
                case JsonPointer jPtr:
                    return TryCoerceJson(jPtr.ResolvedItem, out jType);

                case TJson jVal:
                    jType = jVal;
                    return true;

                default:
                    jType = null;
                    return false;
            }
        }

        private object GetValueFromJsonValue(JsonItem item)
        {
            switch (item)
            {
                case JsonString jStr:
                    return jStr.Value;

                case JsonNumber jNum:
                    return jNum.Value;

                case JsonInteger jInt:
                    return jInt.Value;

                case JsonBoolean jBool:
                    return jBool.Value;

                case JsonNull _:
                    return null;

                case JsonPointer jPtr:
                    return GetValueFromJsonValue(jPtr.ResolvedItem);

                default:
                    throw new ArgumentException($"Cannot get value from JSON type '{item.GetType()}'");
            }
        }

        private JsonSchemaType[] GetSchemaTypeFromObject(JsonObject jObj)
        {
            if (jObj.Fields.TryGetValue("type", out JsonItem typeField))
            {
                return GetSchemaTypeFromFieldValue(typeField);
            }

            if (jObj.Fields.Count == 1)
            {
                if (jObj.Fields.ContainsKey("allOf"))
                {
                    return new[] { JsonSchemaType.AllOf };
                }

                if (jObj.Fields.ContainsKey("anyOf"))
                {
                    return new[] { JsonSchemaType.AnyOf };
                }

                if (jObj.Fields.ContainsKey("oneOf"))
                {
                    return new[] { JsonSchemaType.OneOf };
                }

                if (jObj.Fields.ContainsKey("not"))
                {
                    return new[] { JsonSchemaType.Not };
                }
            }

            // Assume object by default
            return new[] { JsonSchemaType.Object };
        }

        private JsonSchemaType[] GetSchemaTypeFromFieldValue(JsonItem jsonItem)
        {
            switch (jsonItem)
            {
                case JsonString jStr:
                    return new[] { GetSchemaTypeFromString(jStr.Value) };

                case JsonPointer jPtr:
                    return GetSchemaTypeFromFieldValue(jPtr.ResolvedItem);

                case JsonArray jArr:
                    var types = new JsonSchemaType[jArr.Items.Length];
                    for (int i = 0; i < types.Length; i++)
                    {
                        JsonItem element = jArr.Items[i];
                        if (!(element is JsonString jStr))
                        {
                            throw new ArgumentException($"Cannot convert array element of type '{element.GetType()}' to a type moniker");
                        }

                        types[i] = GetSchemaTypeFromString(jStr.Value);
                    }
                    return types;

                default:
                    throw new ArgumentException($"Cannot convert JSON item of type '{jsonItem.GetType()}' to type moniker");
            }
        }

        private JsonSchemaType GetSchemaTypeFromString(string str)
        {
            if (string.Equals(str, "object", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSchemaType.Object;
            }

            if (string.Equals(str, "array", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSchemaType.Array;
            }

            if (string.Equals(str, "string", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSchemaType.String;
            }

            if (string.Equals(str, "boolean", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSchemaType.Boolean;
            }

            if (string.Equals(str, "number", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSchemaType.Number;
            }

            if (string.Equals(str, "integer", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSchemaType.Integer;
            }

            if (string.Equals(str, "null", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSchemaType.Null;
            }

            throw new ArgumentException($"Cannot convert unsupported schema type: '{str}'");
        }

        private StringFormat GetFormatFromString(string str)
        {
            if (string.Equals(str, "date-time", StringComparison.OrdinalIgnoreCase))
            {
                return StringFormat.DateTime;
            }

            if (string.Equals(str, "email", StringComparison.OrdinalIgnoreCase))
            {
                return StringFormat.Email;
            }

            if (string.Equals(str, "hostname", StringComparison.OrdinalIgnoreCase))
            {
                return StringFormat.Hostname;
            }

            if (string.Equals(str, "ipv4", StringComparison.OrdinalIgnoreCase))
            {
                return StringFormat.Ipv4;
            }

            if (string.Equals(str, "ipv6", StringComparison.OrdinalIgnoreCase))
            {
                return StringFormat.Ipv6;
            }

            if (string.Equals(str, "uri", StringComparison.OrdinalIgnoreCase))
            {
                return StringFormat.Uri;
            }

            throw new ArgumentException($"Unsupported format moniker: '{str}'");
        }
    }

    public enum JsonSchemaType
    {
        String,
        Number,
        Integer,
        Object,
        Array,
        Boolean,
        Null,
        AnyOf,
        AllOf,
        OneOf,
        Not,
    }
    
    public abstract class ArmJsonSchema
    {
        public JsonSchemaType[] Type { get; set; }
    }

    public abstract class ArmConcreteSchema : ArmJsonSchema
    {
        public string SchemaVersion { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public object[] Enum { get; set; }

        public object Default { get; set; }
    }

    public class ArmMultitypeSchema : ArmConcreteSchema
    {
    }

    public enum StringFormat
    {
        DateTime,
        Email,
        Hostname,
        Ipv4,
        Ipv6,
        Uri
    }


    public class ArmStringSchema : ArmConcreteSchema
    {
        public ArmStringSchema()
        {
            Type = new[] { JsonSchemaType.String };
        }

        public long? MinLength { get; set; }

        public long? MaxLength { get; set; }

        public string Pattern { get; set; }

        public StringFormat? Format { get; set; }
    }

    public abstract class ArmNumericSchema<TNum> : ArmConcreteSchema where TNum : struct
    {
        public ArmNumericSchema()
        {
            Type = new[] { JsonSchemaType.Number };
        }

        public TNum? MultipleOf { get; set; }

        public TNum? Minimum { get; set; }

        public TNum? Maximum { get; set; }

        public bool ExclusiveMaximum { get; set; }

        public bool ExclusiveMinimum { get; set; }
    }

    public class ArmNumberSchema : ArmNumericSchema<double>
    {
    }

    public class ArmIntegerSchema : ArmNumericSchema<long>
    {
    }

    public class ArmObjectSchema : ArmConcreteSchema
    {
        public ArmObjectSchema()
        {
            Type = new[] { JsonSchemaType.Object };
        }

        public Dictionary<string, ArmJsonSchema> Properties { get; set; }

        public Union<bool, ArmJsonSchema> AdditionalProperties { get; set; }

        public string[] Required { get; set; }

        public long? MinProperties { get; set; }

        public long? MaxProperties { get; set; }
    }

    public abstract class ArmArraySchema : ArmConcreteSchema
    {
        public ArmArraySchema()
        {
            Type = new[] { JsonSchemaType.Array };
        }

        public int? Length { get; set; }

        public bool? UniqueItems { get; set; }
    }

    public class ArmListSchema : ArmArraySchema
    {
        public ArmJsonSchema Items { get; set; }
    }

    public class ArmTupleSchema : ArmArraySchema
    {
        public ArmJsonSchema[] Items { get; set; }

        public Union<bool, ArmJsonSchema> AdditionalItems { get; set; }
    }

    public class ArmBooleanSchema : ArmConcreteSchema
    {
        public ArmBooleanSchema()
        {
            Type = new[] { JsonSchemaType.Boolean };
        }
    }

    public class ArmNullSchema : ArmConcreteSchema
    {
        public ArmNullSchema()
        {
            Type = new[] { JsonSchemaType.Null };
        }
    }

    public abstract class ArmSchemaCombinator : ArmJsonSchema
    {
    }

    public class ArmAnyOfCombinator : ArmSchemaCombinator
    {
        public ArmAnyOfCombinator()
        {
            Type = new[] { JsonSchemaType.AnyOf };
        }

        public ArmJsonSchema[] AnyOf { get; set; }
    }

    public class ArmAllOfCombinator : ArmSchemaCombinator
    {
        public ArmAllOfCombinator()
        {
            Type = new[] { JsonSchemaType.AllOf };
        }

        public ArmJsonSchema[] AllOf { get; set; }
    }

    public class ArmOneOfCombinator : ArmSchemaCombinator
    {
        public ArmOneOfCombinator()
        {
            Type = new[] { JsonSchemaType.OneOf };
        }

        public ArmJsonSchema[] OneOf { get; set; }
    }

    public class ArmNotCombinator : ArmSchemaCombinator
    {
        public ArmNotCombinator()
        {
            Type = new[] { JsonSchemaType.Not };
        }

        public ArmJsonSchema Not { get; set; }
    }
}
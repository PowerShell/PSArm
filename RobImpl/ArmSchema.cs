using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;

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

    /// <summary>
    /// Main entry point; point this to the top level ARM schema URI
    /// and it will pull that down and instantiate it,
    /// creating an object that will lazily and transparently pull down more
    /// on demand.
    /// </summary>
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

            if (schemaTypes == null || schemaTypes.Length == 0)
            {
                schema = CreateUntyped(jObj);
            }
            else if (schemaTypes.Length == 1)
            {
                switch (schemaTypes[0])
                {
                    case JsonSchemaType.AllOf:
                        return CreateAllOf(jObj, CoerceJson<JsonArray>(jObj.Fields["allOf"]));

                    case JsonSchemaType.AnyOf:
                        return CreateAnyOf(jObj, CoerceJson<JsonArray>(jObj.Fields["anyOf"]));

                    case JsonSchemaType.OneOf:
                        return CreateOneOf(jObj, CoerceJson<JsonArray>(jObj.Fields["oneOf"]));

                    case JsonSchemaType.Not:
                        return CreateNot(jObj, CoerceJson<JsonObject>(jObj.Fields["not"]));

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
                        schema = CreateNumeric<ArmNumberSchema, JsonNumber, double>(
                            jObj,
                            (obj) => new ArmNumberSchema(obj));
                        break;

                    case JsonSchemaType.Integer:
                        schema = CreateNumeric<ArmIntegerSchema, JsonInteger, long>(
                            jObj,
                            (obj) => new ArmIntegerSchema(obj));
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

        private ArmAllOfCombinator CreateAllOf(JsonObject parent, JsonArray jArr)
        {
            return new ArmAllOfCombinator(parent)
            {
                AllOf = CreateSchemaArray(jArr.Items),
            };
        }

        private ArmAnyOfCombinator CreateAnyOf(JsonObject parent, JsonArray jArr)
        {
            return new ArmAnyOfCombinator(parent)
            {
                AnyOf = CreateSchemaArray(jArr.Items),
            };
        }

        private ArmOneOfCombinator CreateOneOf(JsonObject parent, JsonArray jArr)
        {
            return new ArmOneOfCombinator(parent)
            {
                OneOf = CreateSchemaArray(jArr.Items),
            };
        }

        private ArmNotCombinator CreateNot(JsonObject parent, JsonObject jObj)
        {
            return new ArmNotCombinator(parent)
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
                return new ArmListSchema(jObj)
                {
                    Length = length,
                    UniqueItems = unique,
                };
            }

            if (TryCoerceJson(itemsField, out JsonObject itemsSchemaObject))
            {
                return new ArmListSchema(jObj)
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

            return new ArmTupleSchema(jObj)
            {
                Length = length,
                UniqueItems = unique,
                AdditionalItems = additionalItems,
                Items = CreateSchemaArray(CoerceJson<JsonArray>(itemsField).Items),
            };
        }

        private ArmBooleanSchema CreateBoolean(JsonObject jObj)
        {
            return new ArmBooleanSchema(jObj);
        }

        private ArmNullSchema CreateNull(JsonObject jObj)
        {
            return new ArmNullSchema(jObj);
        }

        private ArmUntypedSchema CreateUntyped(JsonObject jObj)
        {
            return new ArmUntypedSchema(jObj);
        }

        private TSchema CreateNumeric<TSchema, TJsonNumeric, TNumeric>(
            JsonObject jObj,
            Func<JsonObject, TSchema> factory)
            where TSchema : ArmNumericSchema<TNumeric>
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

            TSchema result = factory(jObj);
            result.MultipleOf = multipleOf;
            result.Minimum = minimum;
            result.Maximum = maximum;
            result.ExclusiveMinimum = exclusiveMinimum;
            result.ExclusiveMaximum = exclusiveMaximum;
            return result;
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

            return new ArmStringSchema(jObj)
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

            return new ArmObjectSchema(jObj)
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
                schema.Enum = enumVals;
            }
        }

        /// <summary>
        /// How we traverse possible JSON pointers.
        /// </summary>
        /// <param name="jItem"></param>
        /// <typeparam name="TJson"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        /// Json pointer traversal when we have a polymorphic JSON item.
        /// </summary>
        /// <param name="jItem"></param>
        /// <param name="jType"></param>
        /// <typeparam name="TJson"></typeparam>
        /// <returns></returns>
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
    
    public abstract class ArmJsonSchema : ICloneable
    {
        protected ArmJsonSchema(JsonItem json)
        {
            Json = json;
        }

        protected ArmJsonSchema(ArmJsonSchema original)
        {
            Type = original.Type;
            Json = original.Json;
        }

        public JsonSchemaType[] Type { get; set; }

        JsonItem Json { get; }

        public abstract object Clone();

        public override string ToString()
        {
            return Json?.ToString() ?? "<null>";
        }
    }

    public abstract class ArmConcreteSchema : ArmJsonSchema
    {
        protected ArmConcreteSchema(JsonItem json)
            : base(json)
        {
        }

        protected ArmConcreteSchema(ArmConcreteSchema original)
            : base(original)
        {
            SchemaVersion = original.SchemaVersion;
            Id = original.Id;
            Title = original.Title;
            Description = original.Description;
            Enum = original.Enum;
            Default = original.Default;
        }

        public string SchemaVersion { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public object[] Enum { get; set; }

        public object Default { get; set; }
    }

    public class ArmMultitypeSchema : ArmConcreteSchema
    {
        public ArmMultitypeSchema(JsonItem json)
            : base(json)
        {
        }

        public ArmMultitypeSchema(ArmMultitypeSchema original)
            : base(original)
        {
        }

        public override object Clone()
        {
            return new ArmMultitypeSchema(this);
        }
    }

    public class ArmUntypedSchema : ArmConcreteSchema
    {
        public ArmUntypedSchema(JsonItem json)
            : base(json)
        {
            Type = Array.Empty<JsonSchemaType>();
        }

        public ArmUntypedSchema(ArmUntypedSchema original)
            : base(original)
        {
        }

        public override object Clone()
        {
            return new ArmUntypedSchema(this);
        }
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
        public ArmStringSchema(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.String };
        }

        public ArmStringSchema(ArmStringSchema original)
            : base(original)
        {
            MinLength = original.MinLength;
            MaxLength = original.MaxLength;
            Pattern = original.Pattern;
            Format = original.Format;
        }

        public long? MinLength { get; set; }

        public long? MaxLength { get; set; }

        public string Pattern { get; set; }

        public StringFormat? Format { get; set; }

        public override object Clone()
        {
            return new ArmStringSchema(this);
        }
    }

    public abstract class ArmNumericSchema<TNum> : ArmConcreteSchema where TNum : struct
    {
        protected ArmNumericSchema(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.Number };
        }

        protected ArmNumericSchema(ArmNumericSchema<TNum> original)
            : base(original)
        {
            MultipleOf = original.MultipleOf;
            Minimum = original.Minimum;
            Maximum = original.Maximum;
            ExclusiveMaximum = original.ExclusiveMaximum;
            ExclusiveMinimum = original.ExclusiveMinimum;
        }

        public TNum? MultipleOf { get; set; }

        public TNum? Minimum { get; set; }

        public TNum? Maximum { get; set; }

        public bool ExclusiveMaximum { get; set; }

        public bool ExclusiveMinimum { get; set; }
    }

    public class ArmNumberSchema : ArmNumericSchema<double>
    {
        public ArmNumberSchema(JsonItem json)
            : base(json)
        {
        }

        public ArmNumberSchema(ArmNumberSchema original)
            : base(original)
        {
        }

        public override object Clone()
        {
            return new ArmNumberSchema(this);
        }
    }

    public class ArmIntegerSchema : ArmNumericSchema<long>
    {
        public ArmIntegerSchema(JsonItem json)
            : base(json)
        {
        }

        public ArmIntegerSchema(ArmIntegerSchema original)
            : base(original)
        {
        }

        public override object Clone()
        {
            return new ArmIntegerSchema(this);
        }
    }

    public class ArmObjectSchema : ArmConcreteSchema
    {
        public ArmObjectSchema(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.Object };
        }

        public ArmObjectSchema(ArmObjectSchema original)
            : base(original)
        {
            if (Properties != null)
            {
                var properties = new Dictionary<string, ArmJsonSchema>(Properties.Count);
                foreach (KeyValuePair<string, ArmJsonSchema> entry in Properties)
                {
                    properties[entry.Key] = (ArmJsonSchema)entry.Value.Clone();
                }

                Properties = properties;
            }

            AdditionalProperties = AdditionalProperties?.Match(
                _ => AdditionalProperties,
                schema => new Union<bool, ArmJsonSchema>.Case2((ArmJsonSchema)schema.Clone()));

            Required = Required;
            MinProperties = MinProperties;
            MaxProperties = MaxProperties;
        }

        public Dictionary<string, ArmJsonSchema> Properties { get; set; }

        public Union<bool, ArmJsonSchema> AdditionalProperties { get; set; }

        public string[] Required { get; set; }

        public long? MinProperties { get; set; }

        public long? MaxProperties { get; set; }

        public override object Clone()
        {
            return new ArmObjectSchema(this);
        }
    }

    public abstract class ArmArraySchema : ArmConcreteSchema
    {
        protected ArmArraySchema(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.Array };
        }

        protected ArmArraySchema(ArmArraySchema original)
            : base(original)
        {
            Length = original.Length;
            UniqueItems = original.UniqueItems;
        }

        public int? Length { get; set; }

        public bool? UniqueItems { get; set; }
    }

    public class ArmListSchema : ArmArraySchema
    {
        public ArmListSchema(JsonItem json)
            : base(json)
        {
        }

        public ArmListSchema(ArmListSchema original)
            : base(original)
        {
            Items = (ArmJsonSchema)original.Items.Clone();
            Length = original.Length;
            UniqueItems = original.UniqueItems;
        }

        public ArmJsonSchema Items { get; set; }

        public override object Clone()
        {
            return new ArmListSchema(this);
        }
    }

    public class ArmTupleSchema : ArmArraySchema
    {
        public ArmTupleSchema(JsonItem json)
            : base(json)
        {
        }

        public ArmTupleSchema(ArmTupleSchema original)
            : base(original)
        {
            var items = new ArmJsonSchema[original.Items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = (ArmJsonSchema)original.Items[i].Clone();
            }

            Items = items;
            AdditionalItems = original.AdditionalItems.Match(
                _ => AdditionalItems,
                schema => new Union<bool, ArmJsonSchema>.Case2((ArmJsonSchema)schema.Clone()));
        }

        public ArmJsonSchema[] Items { get; set; }

        public Union<bool, ArmJsonSchema> AdditionalItems { get; set; }

        public override object Clone()
        {
            return new ArmTupleSchema(this);
        }
    }

    public class ArmBooleanSchema : ArmConcreteSchema
    {
        public ArmBooleanSchema(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.Boolean };
        }

        public ArmBooleanSchema(ArmBooleanSchema original)
            : base(original)
        {
        }

        public override object Clone()
        {
            return new ArmBooleanSchema(this);
        }
    }

    public class ArmNullSchema : ArmConcreteSchema
    {
        public ArmNullSchema(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.Null };
        }

        public ArmNullSchema(ArmNullSchema original)
            : base(original)
        {
        }

        public override object Clone()
        {
            return new ArmNullSchema(this);
        }
    }

    public abstract class ArmSchemaCombinator : ArmJsonSchema
    {
        protected ArmSchemaCombinator(JsonItem json)
            : base(json)
        {
        }

        protected ArmSchemaCombinator(ArmSchemaCombinator original)
            : base(original)
        {
        }
    }

    public class ArmAnyOfCombinator : ArmSchemaCombinator
    {
        public ArmAnyOfCombinator(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.AnyOf };
        }

        public ArmAnyOfCombinator(ArmAnyOfCombinator original)
            : base(original)
        {
            var any = new ArmJsonSchema[original.AnyOf.Length];
            for (int i = 0; i < any.Length; i++)
            {
                any[i] = (ArmJsonSchema)original.AnyOf[i].Clone();
            }

            AnyOf = any;
        }

        public ArmJsonSchema[] AnyOf { get; set; }

        public override object Clone()
        {
            return new ArmAnyOfCombinator(this);
        }
    }

    public class ArmAllOfCombinator : ArmSchemaCombinator
    {
        public ArmAllOfCombinator(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.AllOf };
        }

        public ArmAllOfCombinator(ArmAllOfCombinator original)
            : base(original)
        {
            var all = new ArmJsonSchema[original.AllOf.Length];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = (ArmJsonSchema)original.AllOf[i].Clone();
            }

            AllOf = all;
        }

        public ArmJsonSchema[] AllOf { get; set; }

        public override object Clone()
        {
            return new ArmAllOfCombinator(this);
        }
    }

    public class ArmOneOfCombinator : ArmSchemaCombinator
    {
        public ArmOneOfCombinator(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.OneOf };
        }

        public ArmOneOfCombinator(ArmOneOfCombinator original)
            : base(original)
        {
            var oneOf = new ArmJsonSchema[original.OneOf.Length];
            for (int i = 0; i < oneOf.Length; i++)
            {
                oneOf[i] = (ArmJsonSchema)original.OneOf[i].Clone();
            }

            OneOf = oneOf;
        }

        public ArmJsonSchema[] OneOf { get; set; }

        public override object Clone()
        {
            return new ArmOneOfCombinator(this);
        }
    }

    public class ArmNotCombinator : ArmSchemaCombinator
    {
        public ArmNotCombinator(JsonItem json)
            : base(json)
        {
            Type = new[] { JsonSchemaType.Not };
        }

        public ArmNotCombinator(ArmNotCombinator original)
            : base(original)
        {
            Not = (ArmJsonSchema)original.Not.Clone();
        }

        public ArmJsonSchema Not { get; set; }

        public override object Clone()
        {
            return new ArmNotCombinator(this);
        }
    }
}
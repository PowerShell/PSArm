
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSArm.Schema
{
    // Describes a file or other contained JSON store, rather than a fragment of JSON.
    public class JsonDocument
    {
        private static readonly ConcurrentDictionary<Uri, JsonDocument> s_webDocuments = new ConcurrentDictionary<Uri, JsonDocument>();

        public static JsonDocument FromWebUri(Uri uri)
        {
            return s_webDocuments.GetOrAdd(uri, (documentUri) =>
            {
                Console.WriteLine($"Resolving '{documentUri}'");
                using (var httpClient = new HttpClient())
                using (Stream httpStream = httpClient.GetStreamAsync(documentUri).GetAwaiter().GetResult())

                {
                    return FromStream(httpStream, uri.OriginalString);
                }
            });
        }

        public static JsonDocument FromPath(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return FromStream(fileStream, path);
            }
        }

        public static JsonDocument FromStream(Stream stream, string documentPath)
        {
            JToken root = null;
            using (var textReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                root = JToken.ReadFrom(jsonReader);
            }

            return FromJToken(root, documentPath);
        }

        public static JsonDocument FromJToken(JToken root, string documentPath)
        {
            var document = new JsonDocument(documentPath);

            document.Root = CreateJsonItemFromJToken(document, documentPath, root);

            return document;
        }

        private JsonDocument(string path)
        {
            Path = path;
        }

        public JsonItem Root { get; private set; }

        public string Path { get; }

        public T VisitJson<T>(IJsonItemVisitor<T> visitor)
        {
            return Root.Visit(visitor);
        }

        public void ResolveReferences()
        {
            Root.ResolveReferences();
        }

        public JsonItem ResolveReference(string referencePath)
        {
            if (referencePath.StartsWith("#"))
            {
                return GetLocalReference(referencePath);
            }

            if (Uri.TryCreate(referencePath, UriKind.Absolute, out Uri uri))
            {
                string baseUri = uri.GetLeftPart(UriPartial.Path);

                JsonDocument document = GetReferencedDocument(baseUri);

                return document.GetLocalReference(uri.Fragment);
            }

            throw new NotImplementedException();
        }

        public JsonDocument GetReferencedDocument(string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
            {
                return JsonDocument.FromWebUri(uri);
            }

            throw new NotImplementedException();
        }

        public JsonItem GetLocalReference(string fragmentPath)
        {
            JsonItem item = Root;

            // Skip '#'
            int i = 1;
            int prev = i + 1;
            while (i < fragmentPath.Length)
            {
                i = fragmentPath.IndexOf('/', prev);

                string propertyName = i < 0
                    ? fragmentPath.Substring(prev)
                    : fragmentPath.Substring(prev, i - prev).Replace("~1", "/").Replace("~0", "~");

                item = ((JsonObject)item).Fields[propertyName];

                if (i < 0)
                {
                    break;
                }


                prev = i + 1;
            }

            return item;
        }

        private static JsonItem CreateJsonItemFromJToken(JsonDocument document, string path, JToken currToken)
        {
            JValue currVal;
            switch (currToken.Type)
            {
                case JTokenType.Null:
                    return new JsonNull((JValue)currToken, path);

                case JTokenType.String:
                    currVal = (JValue)currToken;
                    return new JsonString(currVal, path, (string)currVal.Value);

                case JTokenType.Date:
                    currVal = (JValue)currToken;
                    return new JsonString(currVal, path, currVal.Value.ToString());

                case JTokenType.Integer:
                    currVal = (JValue)currToken;
                    return new JsonInteger(currVal, path, (long)currVal.Value);

                case JTokenType.Float:
                    currVal = (JValue)currToken;
                    return new JsonNumber(currVal, path, (double)currVal.Value);

                case JTokenType.Boolean:
                    currVal = (JValue)currToken;
                    return new JsonBoolean(currVal, path, (bool)currVal.Value);

                case JTokenType.Array:
                    var jArray = (JArray)currToken;
                    var arr = new JsonItem[jArray.Count];

                    for (int i = 0; i < jArray.Count; i++)
                    {
                        arr[i] = CreateJsonItemFromJToken(document, $"{path}/{i}", jArray[i]);
                    }

                    return new JsonArray(jArray, path)
                    {
                        Items = arr,
                    };

                case JTokenType.Object:
                    var jObj = (JObject)currToken;

                    // JSON pointer resolution
                    if (jObj.Count == 1
                        && jObj.TryGetValue("$ref", out JToken value)
                        && value is JValue jValue
                        && jValue.Type == JTokenType.String)
                    {
                        return new JsonPointer(jObj, path)
                        {
                            ReferenceUri = (string)jValue,
                            DocumentRoot = document,
                        };
                    }

                    var objDict = new Dictionary<string, JsonItem>(jObj.Count);
                    foreach (KeyValuePair<string, JToken> entry in jObj)
                    {
                        objDict[entry.Key] = CreateJsonItemFromJToken(document, $"{path}/{entry.Key}", entry.Value);
                    }

                    return new JsonObject(jObj, path)
                    {
                        Fields = objDict,
                    };

                default:
                    throw new NotImplementedException($"Unsupported JSON token type '{currToken.Type}' for JSON value '{currToken}'");
            }
        }
    }

    /// <summary>
    /// Wrapper class for JToken, but with transparent JSON pointer traversal.
    /// </summary>
    public abstract class JsonItem
    {
        public JsonItem(JToken json, string path)
        {
            Path = path;
            Json = json;
        }

        public string Path { get; }

        public JToken Json { get; }

        public abstract T Visit<T>(IJsonItemVisitor<T> visitor);

        public virtual void ResolveReferences()
        {
            /*
            var st = new StackTrace();
            if (st.FrameCount > 1000)
            {
                Console.WriteLine(st);
                Debugger.Launch();
            }
            */
        }

        public override string ToString()
        {
            return Json.ToString();
        }
    }

    public class JsonObject : JsonItem
    {
        public JsonObject(JObject jObj, string path)
            : base(jObj, path)
        {
        }

        public Dictionary<string, JsonItem> Fields { get; set; }

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            foreach (JsonItem value in Fields.Values)
            {
                value.ResolveReferences();
            }
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitObject(this);
        }
    }

    public abstract class JsonValue : JsonItem
    {
        public JsonValue(JValue jVal, string path, object value)
            : base(jVal, path)
        {
        }

        public abstract object ValueAsObject { get; }
    }

    public abstract class JsonValue<T> : JsonValue
    {
        public JsonValue(JValue jVal, string path, T value)
            : base(jVal, path, value)
        {
            Value = value;
        }

        public T Value { get; }

        public override object ValueAsObject => Value;
    }

    public class JsonString : JsonValue<string>
    {
        public JsonString(JValue jVal, string path, string value)
            : base(jVal, path, value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitString(this);
        }
    }

    public class JsonInteger : JsonValue<long>
    {
        public JsonInteger(JValue jVal, string path, long value)
            : base(jVal, path, value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitInteger(this);
        }
    }

    public class JsonNumber : JsonValue<double>
    {
        public JsonNumber(JValue jVal, string path, double value)
            : base(jVal, path, value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitNumber(this);
        }
    }

    public class JsonBoolean : JsonValue<bool>
    {
        public JsonBoolean(JValue jVal, string path, bool value)
            : base(jVal, path, value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitBoolean(this);
        }
    }

    public class JsonNull : JsonItem
    {
        public JsonNull(JValue jVal, string path)
            : base(jVal, path)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitNull(this);
        }
    }

    public class JsonArray : JsonItem
    {
        public JsonArray(JArray jArr, string path)
            : base(jArr, path)
        {
        }

        public JsonItem[] Items { get; set; }

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            foreach (JsonItem item in Items)
            {
                item.ResolveReferences();
            }
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitArray(this);
        }
    }

    /// <summary>
    /// This is where JsonPointers are dealt with.
    /// </summary>
    public class JsonPointer : JsonItem
    {
        private readonly Lazy<JsonItem> _resolvedItem;

        private bool _resolved = false;

        public JsonPointer(JObject jObj, string path)
            : base(jObj, path)
        {
            // Set up lazy reference resolution
            _resolvedItem = new Lazy<JsonItem>(() => DocumentRoot.ResolveReference(ReferenceUri));
        }

        public JsonDocument DocumentRoot { get; set; }

        public string ReferenceUri { get; set; }

        public JsonItem ResolvedItem => _resolvedItem.Value;

        public override void ResolveReferences()
        {
            if (!_resolved)
            {
                _resolved = true;
                base.ResolveReferences();
                ResolvedItem.ResolveReferences();
            }
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitPointer(this);
        }
    }
}

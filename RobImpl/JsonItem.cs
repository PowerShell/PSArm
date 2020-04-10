using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RobImpl
{
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

            document.Root = CreateJsonItemFromJToken(document, root);

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

        private static JsonItem CreateJsonItemFromJToken(JsonDocument document, JToken currToken)
        {
            switch (currToken.Type)
            {
                case JTokenType.Null:
                    return JsonNull.Value;

                case JTokenType.String:
                    return new JsonString((string)((JValue)currToken).Value);

                case JTokenType.Date:
                    return new JsonString(((JValue)currToken).Value.ToString());

                case JTokenType.Integer:
                    return new JsonInteger((long)((JValue)currToken).Value);

                case JTokenType.Float:
                    return new JsonNumber((double)((JValue)currToken).Value);

                case JTokenType.Boolean:
                    return new JsonBoolean((bool)((JValue)currToken).Value);

                case JTokenType.Array:
                    var jarray = (JArray)currToken;
                    var arr = new JsonItem[jarray.Count];

                    for (int i = 0; i < jarray.Count; i++)
                    {
                        arr[i] = CreateJsonItemFromJToken(document, jarray[i]);
                    }

                    return new JsonArray
                    {
                        Items = arr,
                    };

                case JTokenType.Object:
                    var jobj = (IDictionary<string, JToken>)currToken;

                    // JSON pointer resolution
                    if (jobj.Count == 1
                        && jobj.TryGetValue("$ref", out JToken value)
                        && value is JValue jValue
                        && jValue.Type == JTokenType.String)
                    {
                        return new JsonPointer
                        {
                            ReferenceUri = (string)jValue,
                            DocumentRoot = document,
                        };
                    }

                    var objDict = new Dictionary<string, JsonItem>(jobj.Count);
                    foreach (KeyValuePair<string, JToken> entry in jobj)
                    {
                        objDict[entry.Key] = CreateJsonItemFromJToken(document, entry.Value);
                    }

                    return new JsonObject
                    {
                        Fields = objDict,
                    };

                default:
                    throw new NotImplementedException($"Unsupported JSON token type '{currToken.Type}' for JSON value '{currToken}'");
            }
        }
    }

    public abstract class JsonItem
    {
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
    }

    public class JsonObject : JsonItem
    {
        public JsonObject()
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

    public abstract class JsonValue<T> : JsonItem
    {
        public JsonValue(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }

    public class JsonString : JsonValue<string>
    {
        public JsonString(string value) : base(value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitString(this);
        }
    }

    public class JsonInteger : JsonValue<long>
    {
        public JsonInteger(long value) : base(value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitInteger(this);
        }
    }

    public class JsonNumber : JsonValue<double>
    {
        public JsonNumber(double value) : base(value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitNumber(this);
        }
    }

    public class JsonBoolean : JsonValue<bool>
    {
        public JsonBoolean(bool value) : base(value)
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitBoolean(this);
        }
    }

    public class JsonNull : JsonItem
    {
        public static JsonNull Value { get; } = new JsonNull();

        private JsonNull()
        {
        }

        public override T Visit<T>(IJsonItemVisitor<T> visitor)
        {
            return visitor.VisitNull(this);
        }
    }

    public class JsonArray : JsonItem
    {
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

    public class JsonPointer : JsonItem
    {
        private readonly Lazy<JsonItem> _resolvedItem;

        private bool _resolved = false;

        public JsonPointer()
        {
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

[JsonConverter(typeof(StringEnumConverter))]
public enum DslSchemaKind
{
    [EnumMember(Value = "block")]
    Block,
    [EnumMember(Value = "array")]
    Array,
    [EnumMember(Value = "command")]
    Command,
    [EnumMember(Value = "bodycommand")]
    BodyCommand,
}

public class DslParameter
{
    public DslParameter()
    {
        Enum = new List<object>();
    }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("enum")]
    public List<object> Enum { get; set; }
}

public abstract class DslSchemaItem
{
    [JsonProperty("kind")]
    public abstract DslSchemaKind Kind { get; }

    [JsonProperty("parameters")]
    public List<DslParameter> Parameters { get; set; }

    public abstract void Visit(string commandName, IDslSchemaVisitor visitor);
}

public class DslBlockSchema : DslSchemaItem
{
    public DslBlockSchema()
    {
        Body = new Dictionary<string, DslSchemaItem>();
    }

    [JsonProperty("kind")]
    public override DslSchemaKind Kind => DslSchemaKind.Block;

    [JsonProperty("body")]
    public Dictionary<string, DslSchemaItem> Body { get; set; }

    public override void Visit(string commandName, IDslSchemaVisitor visitor)
    {
        visitor.VisitBlockKeyword(commandName, this);
    }
}

public class DslArraySchema : DslSchemaItem
{
    [JsonProperty("kind")]
    public override DslSchemaKind Kind => DslSchemaKind.Array;

    [JsonProperty("body")]
    public Dictionary<string, DslSchemaItem> Body { get; set; }

    public override void Visit(string commandName, IDslSchemaVisitor visitor)
    {
        visitor.VisitArrayKeyword(commandName, this);
    }
}

public class DslCommandSchema : DslSchemaItem
{
    [JsonProperty("kind")]
    public override DslSchemaKind Kind => DslSchemaKind.Command;

    public override void Visit(string commandName, IDslSchemaVisitor visitor)
    {
        visitor.VisitCommandKeyword(commandName, this);
    }
}

public class DslBodyCommandSchema : DslSchemaItem
{
    [JsonProperty("kind")]
    public override DslSchemaKind Kind => DslSchemaKind.BodyCommand;

    public override void Visit(string commandName, IDslSchemaVisitor visitor)
    {
        visitor.VisitBodyCommandKeyword(commandName, this);
    }
}


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

public class DslSchema
{
    public DslSchema(string name, Dictionary<string, Dictionary<string, DslSchemaItem>> subschemas)
    {
        Name = name;
        Subschemas = subschemas;
    }

    public string Name { get; }

    public Dictionary<string, Dictionary<string, DslSchemaItem>> Subschemas { get; }
}

public class DslSchemaReader
{
    private readonly JsonSerializer _jsonSerializer;

    public DslSchemaReader()
    {
        _jsonSerializer = new JsonSerializer()
        {
            Converters = { new DslSchemaConverter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };
    }

    public DslSchema ReadSchema(string path)
    {
        Dictionary<string, Dictionary<string, DslSchemaItem>> subschemas = null;
        using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var textReader = new StreamReader(file))
        using (var jsonReader = new JsonTextReader(textReader))
        {
            subschemas = _jsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DslSchemaItem>>>(jsonReader);
        }

        string schemaNamspace = Path.GetFileNameWithoutExtension(path);

        return new DslSchema(schemaNamspace, subschemas);
    }
}

public interface IDslSchemaVisitor
{
    void VisitCommandKeyword(string commandName, DslCommandSchema command);

    void VisitBodyCommandKeyword(string commandName, DslBodyCommandSchema bodyCommand);

    void VisitArrayKeyword(string commandName, DslArraySchema array);

    void VisitBlockKeyword(string commandName, DslBlockSchema block);
}

public class DslScriptWriter : IDslSchemaVisitor
{
    private readonly StringBuilder _sb;

    private int _indent = 0;

    public DslScriptWriter()
    {
        _sb = new StringBuilder();
    }

    public Dictionary<string, string> WriteDslDefinitions(DslSchema schema)
    {
        var dict = new Dictionary<string, string>();
        foreach (KeyValuePair<string, Dictionary<string, DslSchemaItem>> entry in schema.Subschemas)
        {
            string schemaName = $"{schema.Name}/{entry.Key}";

            if (entry.Value.Count == 0)
            {
                dict[schemaName] = string.Empty;
                continue;
            }

            foreach (KeyValuePair<string, DslSchemaItem> topKeyword in entry.Value)
            {
                Reset();
                topKeyword.Value.Visit(topKeyword.Key, this);
                dict[schemaName] = _sb.ToString();
            }
        }
        return dict;
    }

    public void Reset()
    {
        _sb.Clear();
        _indent = 0;
    }

    public void VisitCommandKeyword(string commandName, DslCommandSchema command)
    {
        WriteFunctionBeginning(commandName, command.Parameters);

        _sb.Append("Value ");
        WriteLiteral(UnPascal(commandName));
        _sb.Append(' ');
        WriteVariable(command.Parameters[0].Name);

        WriteFunctionEnd();
    }

    public void VisitBodyCommandKeyword(string commandName, DslBodyCommandSchema bodyCommand)
    {
        WriteFunctionBeginning(commandName, bodyCommand.Parameters);

        _sb.Append("Composite ");
        WriteLiteral(UnPascal(commandName));
        _sb.Append(" $PSBoundParameters");

        WriteFunctionEnd();
    }

    public void VisitArrayKeyword(string commandName, DslArraySchema array)
    {
        WriteFunctionBeginning(commandName, array.Parameters, writeBodyParameter: array.Body != null);

        if (array.Body != null)
        {
            foreach (KeyValuePair<string, DslSchemaItem> subSchema in array.Body)
            {
                subSchema.Value.Visit(subSchema.Key, this);
                Newline();
            }
        }

        _sb.Append("ArrayItem ");
        WriteLiteral(UnPascal(commandName));
        _sb.Append(" $PSBoundParameters $Body");

        WriteFunctionEnd();
    }

    public void VisitBlockKeyword(string commandName, DslBlockSchema block)
    {
        WriteFunctionBeginning(commandName, block.Parameters, writeBodyParameter: true);

        foreach (KeyValuePair<string, DslSchemaItem> subSchema in block.Body)
        {
            subSchema.Value.Visit(subSchema.Key, this);
            Newline();
        }

        _sb.Append("Block ");
        WriteLiteral(UnPascal(commandName));
        _sb.Append(" $PSBoundParameters $Body");

        WriteFunctionEnd();
    }

    private void WriteFunctionBeginning(string functionName, IReadOnlyList<DslParameter> parameters, bool writeBodyParameter = false)
    {
        _sb.Append("function ").Append(functionName);

        StartBlock();

        _sb.Append("[CmdletBinding()]");
        Newline();
        _sb.Append("param(");
        Indent();
        Newline();

        if (parameters != null)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                DslParameter parameter = parameters[i];
                WriteParameter(parameter, position: i);

                if (i < parameters.Count - 1)
                {
                    _sb.Append(',');
                    Newline();
                    Newline();
                }
                else if (writeBodyParameter)
                {
                    _sb.Append(',');
                    Newline();
                    Newline();
                    WriteParameter("Body", "scriptblock", position: i + 1, validationSet: null);
                }
            }
        }

        Dedent();
        Newline();
        _sb.Append(')');
        Newline();
        Newline();
    }

    private void WriteParameter(DslParameter parameter, int position)
    {
        WriteParameter(parameter.Name, parameter.Type, position, parameter.Enum);
    }

    private void WriteParameter(string name, string type, int position, IReadOnlyList<object> validationSet)
    {
        _sb.Append("[Parameter(Position = ").Append(position).Append(", Mandatory)]");
        Newline();
        WriteVariable(name);
    }

    private void WriteFunctionEnd()
    {
        EndBlock();
        Newline();
    }

    private void WriteLiteral(object value)
    {
        switch (value)
        {
            case string s:
                _sb.Append('\'').Append(s.Replace("'", "''")).Append('\'');
                return;

            case bool b:
                _sb.Append(b ? "$true" : "$false");
                return;

            case int i:
                _sb.Append(i);
                return;

            case long l:
                _sb.Append(l);
                return;

            case double d:
                _sb.Append(d);
                return;

            default:
                throw new NotImplementedException();
        }
    }

    private void WriteVariable(string variableName)
    {
        _sb.Append('$').Append(variableName);
    }

    private string UnPascal(string s)
    {
        return char.IsUpper(s[0])
            ? char.ToLower(s[0]) + s.Substring(1)
            : s;
    }

    private string Pluralise(string s)
    {
        return s + "s";
    }

    private void Indent()
    {
        _indent++;
    }

    private void Dedent()
    {
        _indent--;
    }

    private void Newline()
    {
        _sb.Append('\n').Append(' ', 4 * _indent);
    }

    private void StartBlock()
    {
        Newline();
        _sb.Append('{');
        Indent();
        Newline();
    }

    private void EndBlock()
    {
        Dedent();
        Newline();
        _sb.Append('}');
    }
}

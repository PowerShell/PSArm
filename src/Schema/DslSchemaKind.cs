using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace PSArm.Schema
{
    /// <summary>
    /// Possible kinds of ARM DSL schema.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DslSchemaKind
    {
        /// <summary>
        /// A keyword with a scriptblock body.
        /// </summary>
        [EnumMember(Value = "block")]
        Block,

        /// <summary>
        /// A keyword with block body that contributes to an array.
        /// </summary>
        [EnumMember(Value = "array")]
        Array,

        /// <summary>
        /// A simple command keyword corresponding to a JSON key value pair.
        /// </summary>
        [EnumMember(Value = "command")]
        Command,

        /// <summary>
        /// A command keyword corresponding to a JSON key with an object body.
        /// </summary>
        [EnumMember(Value = "bodycommand")]
        BodyCommand,
    }
}

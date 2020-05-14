using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PSArm.Schema
{
    /// <summary>
    /// ARM DSL schema element for a command contributing array items.
    /// </summary>
    public class DslArraySchema : DslSchemaItem
    {
        /// <summary>
        /// The kind of this DSL schema element: array.
        /// </summary>
        [JsonProperty("kind")]
        public override DslSchemaKind Kind => DslSchemaKind.Array;

        /// <summary>
        /// The body description of this keyword. May be null, in which case this is a command keyword.
        /// </summary>
        [JsonProperty("body")]
        public Dictionary<string, DslSchemaItem> Body { get; set; }

        public override void Visit(string commandName, IDslSchemaVisitor visitor)
        {
            visitor.VisitArrayKeyword(commandName, this);
        }
    }
}

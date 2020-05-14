using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PSArm.Schema
{
    /// <summary>
    /// An ARM DSL command keyword that generates a key value pair with an object value in ARM template JSON.
    /// </summary>
    public class DslBodyCommandSchema : DslSchemaItem
    {
        /// <summary>
        /// The kind of this keyword: bodyCommand.
        /// </summary>
        [JsonProperty("kind")]
        public override DslSchemaKind Kind => DslSchemaKind.BodyCommand;

        public override void Visit(string commandName, IDslSchemaVisitor visitor)
        {
            visitor.VisitBodyCommandKeyword(commandName, this);
        }
    }
}

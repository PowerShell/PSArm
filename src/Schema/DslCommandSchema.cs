
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PSArm.Schema
{
    /// <summary>
    /// An ARM DSL command keyword that represents a key/simple-value pair in ARM template JSON.
    /// </summary>
    public class DslCommandSchema : DslSchemaItem
    {
        /// <summary>
        /// The kind of this keyword: command.
        /// </summary>
        [JsonProperty("kind")]
        public override DslSchemaKind Kind => DslSchemaKind.Command;

        public override void Visit(string commandName, IDslSchemaVisitor visitor)
        {
            visitor.VisitCommandKeyword(commandName, this);
        }
    }
}

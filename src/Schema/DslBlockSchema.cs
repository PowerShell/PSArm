
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PSArm.Schema
{
    /// <summary>
    /// DSL schema node for a scriptblock keyword.
    /// </summary>
    public class DslBlockSchema : DslSchemaItem
    {
        /// <summary>
        /// Create a new block schema item.
        /// </summary>
        public DslBlockSchema()
        {
            Body = new Dictionary<string, DslSchemaItem>();
        }

        /// <summary>
        /// The kind of this schem item: block.
        /// </summary>
        [JsonProperty("kind")]
        public override DslSchemaKind Kind => DslSchemaKind.Block;

        /// <summary>
        /// The schema of the body of this item.
        /// </summary>
        [JsonProperty("body")]
        public Dictionary<string, DslSchemaItem> Body { get; set; }

        /// <summary>
        /// Visit this item with a visitor.
        /// </summary>
        /// <param name="commandName">The command name of this item.</param>
        /// <param name="visitor">The visitor visiting this item.</param>
        public override void Visit(string commandName, IDslSchemaVisitor visitor)
        {
            visitor.VisitBlockKeyword(commandName, this);
        }
    }
}

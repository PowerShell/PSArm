
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PSArm.Schema
{
    /// <summary>
    /// A node in the ARM DSL schema.
    /// </summary>
    public abstract class DslSchemaItem
    {
        /// <summary>
        /// The kind of node this is.
        /// </summary>
        [JsonProperty("kind")]
        public abstract DslSchemaKind Kind { get; }

        /// <summary>
        /// Any parameters on this item.
        /// </summary>
        [JsonProperty("parameters")]
        public List<DslParameter> Parameters { get; set; }

        /// <summary>
        /// Visitor method for a schema vistor.
        /// </summary>
        /// <param name="commandName">The command name associated with this node.</param>
        /// <param name="visitor">The schema visitor.</param>
        public abstract void Visit(string commandName, IDslSchemaVisitor visitor);
    }
}

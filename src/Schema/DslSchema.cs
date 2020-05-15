
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;

namespace PSArm.Schema
{
    /// <summary>
    /// Describes an ARM resource DSL schema.
    /// </summary>
    public class DslSchema
    {
        /// <summary>
        /// Create a new ARM resource DSL schema.
        /// </summary>
        /// <param name="name">The ARM resource namespace this schema provides a DSL for.</param>
        /// <param name="subschemas">Schemas of constituent keywords of this schema.</param>
        public DslSchema(string name, Dictionary<string, Dictionary<string, DslSchemaItem>> subschemas)
        {
            Name = name;
            Subschemas = subschemas;
        }

        /// <summary>
        /// The ARM resource namespace this schema provides a DSL for.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Schemas of constituent keywords of this schema.
        /// </summary>
        public Dictionary<string, Dictionary<string, DslSchemaItem>> Subschemas { get; }
    }
}

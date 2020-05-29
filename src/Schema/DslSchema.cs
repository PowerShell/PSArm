
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
        /// <param name="name">The ARM resource provider this schema provides a DSL for.</param>
        /// <param name="subschemas">Schemas of constituent keywords of this schema.</param>
        public DslSchema(string providerName, string apiVersion, Dictionary<string, Dictionary<string, DslSchemaItem>> subschemas)
        {
            ProviderName = providerName;
            ApiVersion = apiVersion;
            Subschemas = subschemas;
        }

        /// <summary>
        /// The ARM resource namespace this schema provides a DSL for.
        /// </summary>
        public string ProviderName { get; }

        public string ApiVersion { get;  }

        /// <summary>
        /// Schemas of constituent keywords of this schema.
        /// </summary>
        public Dictionary<string, Dictionary<string, DslSchemaItem>> Subschemas { get; }
    }
}


// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;

namespace PSArm.Schema
{
    /// <summary>
    /// Describes a complete ARM DSL schema loaded from a description file.
    /// </summary>
    public class ArmDslInfo
    {
        /// <summary>
        /// Create a new ARM DSL info object.
        /// </summary>
        /// <param name="schema">The keyword schema for the DSL segment.</param>
        /// <param name="dslScripts">The PowerShell scripts for the DSL, keyed by resource type name.</param>
        public ArmDslInfo(DslSchema schema, IReadOnlyDictionary<string, string> dslScripts)
        {
            Schema = schema;
            DslDefintions = dslScripts;
        }

        /// <summary>
        /// The keyword schema for this DSL component.
        /// </summary>
        public DslSchema Schema { get; }

        /// <summary>
        /// The PowerShell scripts for the DSL, keyed by resource type name.
        /// </summary>
        public IReadOnlyDictionary<string, string> DslDefintions { get; }
    }
}

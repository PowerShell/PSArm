
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;

namespace PSArm.Schema
{
    /// <summary>
    /// Describes a complete ARM DSL schema loaded from a description file.
    /// </summary>
    public class ArmProviderDslInfo
    {
        public ArmProviderDslInfo(ArmDslProviderSchema providerSchema)
        {
            ProviderSchema = providerSchema;
            ScriptProducer = new ArmDslProviderScriptProducer(providerSchema);
        }

        /// <summary>
        /// The keyword schema for this DSL component.
        /// </summary>
        public ArmDslProviderSchema ProviderSchema { get; }

        public ArmDslProviderScriptProducer ScriptProducer { get; }
    }
}

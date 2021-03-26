
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation;

namespace PSArm.Schema
{
    public class ResourceDslDefinition
    {
        public ResourceDslDefinition(
            Dictionary<string, ScriptBlock> resourceKeywordDefinitions)
            : this(resourceKeywordDefinitions, discriminatedKeywordDefinitions: null)
        {
        }

        public ResourceDslDefinition(
            Dictionary<string, ScriptBlock> resourceKeywordDefinitions,
            IReadOnlyDictionary<string, Dictionary<string, ScriptBlock>> discriminatedKeywordDefinitions)
        {
            ResourceKeywordDefinitions = resourceKeywordDefinitions;
            DiscriminatedKeywordDefinitions = discriminatedKeywordDefinitions;
        }

        public Dictionary<string, ScriptBlock> ResourceKeywordDefinitions { get; }

        public IReadOnlyDictionary<string, Dictionary<string, ScriptBlock>> DiscriminatedKeywordDefinitions { get; }
    }
}

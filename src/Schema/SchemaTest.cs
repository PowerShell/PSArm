
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PSArm.Schema
{
    public static class SchemaTest
    {
        public static void Run()
        {
            var index = new ResourceIndex();
            var factory = new PSArmDslFactory();
            foreach (ResourceSchema schema in index.GetResourceSchemas())
            {
                factory.CreateResourceDslDefinition(schema.Properties, schema.DiscriminatedSubtypes);
            }
        }
    }
}

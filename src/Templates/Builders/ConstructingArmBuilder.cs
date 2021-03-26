
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Primitives;

namespace PSArm.Templates.Builders
{
    public class ConstructingArmBuilder<T> : ArmBuilder<T> where T : ArmObject, new()
    {
        public ConstructingArmBuilder() : base(new T())
        {
        }
    }
}

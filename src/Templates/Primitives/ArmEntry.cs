
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PSArm.Templates.Primitives
{
    public class ArmEntry
    {
        public ArmEntry(IArmString key, ArmElement value) : this(key, value, isArrayElement: false)
        {
        }

        public ArmEntry(IArmString key, ArmElement value, bool isArrayElement)
        {
            Key = key;
            Value = value;
            IsArrayElement = isArrayElement;
        }

        public IArmString Key { get; }

        public ArmElement Value { get; }

        public bool IsArrayElement { get; }
    }
}

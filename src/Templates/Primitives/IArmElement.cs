
// Copyright (c) Microsoft Corporation.

using System.Collections.Generic;

namespace PSArm.Templates.Primitives
{
    public interface IArmElement
    {
        IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters);
    }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PSArm.Templates.Primitives
{
    public interface IArmElement
    {
        IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters);
    }
}

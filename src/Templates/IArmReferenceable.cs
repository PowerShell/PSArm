﻿using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    public interface IArmReferenceable
    {
        IArmString ReferenceName { get; }
    }

    public interface IArmReferenceable<TReference> : IArmReferenceable
    {
        TReference GetReference();
    }
}

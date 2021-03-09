
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using System.Collections.Generic;

namespace PSArm.Templates.Metadata
{
    public class ArmMetadata : ArmObject
    {
        public IArmString Comments
        {
            get => (IArmString)GetElementOrNull(ArmTemplateKeys.Comments);
            set => this[ArmTemplateKeys.Comments] = (ArmElement)value;
        }

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new ArmMetadata(), parameters);
    }
}

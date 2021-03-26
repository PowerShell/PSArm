
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Primitives;
using System.Collections.Generic;

namespace PSArm.Templates.Metadata
{
    public class PSArmTopLevelTemplateMetadata : ArmMetadata
    {
        public PSArmTopLevelTemplateMetadata()
        {
            GeneratorMetadata = new PSArmGeneratorMetadata();
        }

        public PSArmGeneratorMetadata GeneratorMetadata
        {
            get => (PSArmGeneratorMetadata)GetElementOrNull(ArmTemplateKeys.GeneratorKey);
            set => this[ArmTemplateKeys.GeneratorKey] = value;
        }

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new PSArmTopLevelTemplateMetadata(), parameters);
    }
}

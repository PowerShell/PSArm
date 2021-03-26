
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Primitives;
using System.Collections.Generic;

namespace PSArm.Templates.Metadata
{
    public class ArmGeneratorMetadata : ArmObject<ArmStringLiteral>
    {
        public ArmStringLiteral Name
        {
            get => GetElementOrNull(ArmTemplateKeys.Name);
            set => this[ArmTemplateKeys.Name] = value;
        }

        public ArmStringLiteral Version
        {
            get => GetElementOrNull(ArmTemplateKeys.Version);
            set => this[ArmTemplateKeys.Version] = value;
        }

        public ArmStringLiteral TemplateHash
        {
            get => GetElementOrNull(ArmTemplateKeys.TemplateHash);
            set => this[ArmTemplateKeys.TemplateHash] = value;
        }

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new ArmGeneratorMetadata(), parameters);
    }
}

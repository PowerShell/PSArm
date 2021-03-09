
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;
using System.Collections.Generic;

namespace PSArm.Templates.Metadata
{
    public class PSArmGeneratorMetadata : ArmGeneratorMetadata
    {
        private static readonly ArmStringLiteral s_psarmGeneratorName = new ArmStringLiteral("psarm");

        private static readonly ArmStringLiteral s_psarmGeneratorVersion = new ArmStringLiteral(typeof(PSArmGeneratorMetadata).Assembly.GetName().Version.ToString());

        public PSArmGeneratorMetadata()
        {
            Name = s_psarmGeneratorName;
            Version = s_psarmGeneratorVersion;
        }

        public ArmStringLiteral PowerShellVersion
        {
            get => GetElementOrNull(ArmTemplateKeys.Metadata_PSVersion);
            set => this[ArmTemplateKeys.Metadata_PSVersion] = value;
        }

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new PSArmGeneratorMetadata(), parameters);
    }
}

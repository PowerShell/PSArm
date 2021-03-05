
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Primitives;

namespace PSArm.Templates
{
    public class ArmTemplateResource : ArmResource
    {
        private static readonly ArmStringLiteral s_type = new ArmStringLiteral("Microsoft.Resources/deployments");
        private static readonly ArmStringLiteral s_apiVersion = new ArmStringLiteral("2019-10-01");
        private static readonly ArmStringLiteral s_incrementalMode = new ArmStringLiteral("Incremental");

        public ArmTemplateResource(IArmString name)
        {
            Name = name;
            Type = s_type;
            ApiVersion = s_apiVersion;
            this[ArmTemplateKeys.Properties] = new ArmObject
            {
                [ArmTemplateKeys.Mode] = s_incrementalMode,
            };
        }

        public ArmTemplate Template
        {
            get => (ArmTemplate)((ArmObject)GetElementOrNull(ArmTemplateKeys.Properties))?[ArmTemplateKeys.Template];
            set => ((ArmObject)this[ArmTemplateKeys.Properties])[ArmTemplateKeys.Template] = value;
        }
    }
}

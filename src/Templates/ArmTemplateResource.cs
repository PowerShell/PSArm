
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Templates
{
    public class ArmTemplateResource : ArmResource
    {
        private static readonly ArmStringLiteral s_type = new ArmStringLiteral("Microsoft.Resources/deployments");
        private static readonly ArmStringLiteral s_apiVersion = new ArmStringLiteral("2019-10-01");
        private static readonly ArmStringLiteral s_incrementalMode = new ArmStringLiteral("Incremental");
        private static readonly ArmStringLiteral s_inner = new ArmStringLiteral("inner");

        public ArmTemplateResource(IArmString name)
        {
            Name = name;
            Type = s_type;
            ApiVersion = s_apiVersion;
            this[ArmTemplateKeys.Properties] = new ArmObject
            {
                [ArmTemplateKeys.Mode] = s_incrementalMode,
                [ArmTemplateKeys.ExpressionEvaluationOptions] = new ArmObject
                {
                    [ArmTemplateKeys.Scope] = s_inner,
                },
            };
        }

        public ArmTemplate Template
        {
            get => (ArmTemplate)((ArmObject)GetElementOrNull(ArmTemplateKeys.Properties))?[ArmTemplateKeys.Template];
            set => ((ArmObject)this[ArmTemplateKeys.Properties])[ArmTemplateKeys.Template] = value;
        }

        protected override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitTemplateResource(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new ArmTemplateResource((IArmString)Name.Instantiate(parameters)), parameters);
    }
}

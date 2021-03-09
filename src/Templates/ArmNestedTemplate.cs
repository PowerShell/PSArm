
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Metadata;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Templates
{
    public class ArmNestedTemplate : ArmTemplate
    {
        public static ArmNestedTemplate CreateFromTemplates(IEnumerable<ArmTemplate> templates)
        {
            var resourceArray = new ArmArray<ArmResource>();
            var templateNames = new Dictionary<string, int>();
            foreach (ArmTemplate template in templates)
            {
                string templateName = template.TemplateName;
                if (templateNames.TryGetValue(templateName, out int count))
                {
                    count++;
                    templateName = $"{templateName}_{count}";
                    templateNames[templateName] = count;
                }
                else
                {
                    templateNames[templateName] = 0;
                }

                resourceArray.Add(new ArmTemplateResource(new ArmStringLiteral(templateName))
                {
                    Template = template,
                });
            }

            return new ArmNestedTemplate
            {
                Resources = resourceArray,
            };
        }

        public ArmNestedTemplate()
        {
            Metadata = new PSArmTopLevelTemplateMetadata();
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitNestedTemplate(this);

        public override IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters)
            => InstantiateIntoCopy(new ArmNestedTemplate(), parameters);
    }
}

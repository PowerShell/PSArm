
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Primitives;
using System.Collections.Generic;

namespace PSArm.Templates.Builders
{
    internal class ArmNestedTemplateBuilder
    {
        private readonly ArmArray<ArmResource> _templateResources;

        private readonly Dictionary<string, int> _templateNameCounts;

        public ArmNestedTemplateBuilder()
        {
            _templateResources = new ArmArray<ArmResource>();
            _templateNameCounts = new Dictionary<string, int>();
        }

        public ArmNestedTemplateBuilder AddTemplate(ArmTemplate template)
        {
            string templateName = template.TemplateName;
            if (_templateNameCounts.TryGetValue(templateName, out int count))
            {
                count++;
                templateName = $"{templateName}_{count}";
                _templateNameCounts[templateName] = count;
            }
            else
            {
                _templateNameCounts[templateName] = 0;
            }

            _templateResources.Add(new ArmTemplateResource(new ArmStringLiteral(templateName))
            {
                Template = template,
            });

            return this;
        }
        
        public ArmNestedTemplate Build()
        {
            return new ArmNestedTemplate
            {
                Resources = _templateResources,
            };
        }

        public void Clear()
        {
            _templateResources.Clear();
            _templateNameCounts.Clear();
        }
    }
}

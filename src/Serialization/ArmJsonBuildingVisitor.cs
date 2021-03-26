
// Copyright (c) Microsoft Corporation.

using Newtonsoft.Json.Linq;
using PSArm.Templates;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System.Collections.Generic;

namespace PSArm.Serialization
{
    public class ArmJsonBuildingVisitor : IArmVisitor<JToken>
    {
        public JToken VisitArray(ArmArray array)
        {
            var arr = new JArray();
            foreach (ArmElement element in array)
            {
                arr.Add(element.RunVisit(this));
            }
            return arr;
        }

        public JToken VisitBooleanValue(ArmBooleanLiteral booleanValue) => new JValue(booleanValue.Value);

        public JToken VisitDoubleValue(ArmDoubleLiteral doubleValue) => new JValue(doubleValue.Value);

        public JToken VisitFunctionCall(ArmFunctionCallExpression functionCall) => VisitArmString(functionCall);

        public JToken VisitIndexAccess(ArmIndexAccessExpression indexAccess) => VisitArmString(indexAccess);

        public JToken VisitIntegerValue(ArmIntegerLiteral integerValue) => new JValue(integerValue.Value);

        public JToken VisitMemberAccess(ArmMemberAccessExpression memberAccess) => VisitArmString(memberAccess);

        public JToken VisitNestedTemplate(ArmNestedTemplate nestedTemplate) => VisitTemplate(nestedTemplate);

        public JToken VisitNullValue(ArmNullLiteral nullValue) => VisitFunctionCall(nullValue);

        public JToken VisitObject(ArmObject obj)
        {
            var jObj = new JObject();
            foreach (KeyValuePair<IArmString, ArmElement> entry in obj)
            {
                if (entry.Value is null)
                {
                    continue;
                }

                jObj[entry.Key.ToExpressionString()] = entry.Value.RunVisit(this);
            }
            return jObj;
        }

        public JToken VisitOutput(ArmOutput output) => VisitObject(output);

        public JToken VisitParameterDeclaration(ArmParameter parameter) => VisitObject(parameter);

        public JToken VisitParameterReference(ArmParameterReferenceExpression parameterReference) => VisitArmString(parameterReference);

        public JToken VisitResource(ArmResource resource) => VisitObject(resource);

        public JToken VisitSku(ArmSku sku) => VisitObject(sku);

        public JToken VisitStringValue(ArmStringLiteral stringValue) => new JValue(stringValue.Value);

        public JToken VisitTemplate(ArmTemplate template) => VisitObject(template);

        public JToken VisitTemplateResource(ArmTemplateResource templateResource) => VisitResource(templateResource);

        public JToken VisitVariableDeclaration(ArmVariable variable) => variable.Value?.RunVisit(this);

        public JToken VisitVariableReference(ArmVariableReferenceExpression variableReference) => VisitArmString(variableReference);

        private JToken VisitArmString(IArmString str)
        {
            return new JValue(str.ToExpressionString());
        }
    }
}

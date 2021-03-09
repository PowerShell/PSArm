
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;

namespace PSArm.Templates.Visitors
{
    public interface IArmVisitor<out TResult>
    {
        TResult VisitStringValue(ArmStringLiteral stringValue);

        TResult VisitNullValue(ArmNullLiteral nullValue);

        TResult VisitIntegerValue(ArmIntegerLiteral integerValue);

        TResult VisitDoubleValue(ArmDoubleLiteral doubleValue);

        TResult VisitBooleanValue(ArmBooleanLiteral booleanValue);

        TResult VisitArray(ArmArray array);

        TResult VisitObject(ArmObject obj);

        TResult VisitFunctionCall(ArmFunctionCallExpression functionCall);

        TResult VisitMemberAccess(ArmMemberAccessExpression memberAccess);

        TResult VisitIndexAccess(ArmIndexAccessExpression indexAccess);

        TResult VisitParameterReference(ArmParameterReferenceExpression parameterReference);

        TResult VisitVariableReference(ArmVariableReferenceExpression variableReference);

        TResult VisitTemplate(ArmTemplate template);

        TResult VisitNestedTemplate(ArmNestedTemplate nestedTemplate);

        TResult VisitTemplateResource(ArmTemplateResource templateResource);

        TResult VisitOutput(ArmOutput output);

        TResult VisitResource(ArmResource resource);

        TResult VisitSku(ArmSku sku);

        TResult VisitParameterDeclaration(ArmParameter parameter);

        TResult VisitVariableDeclaration(ArmVariable variable);
    }
}

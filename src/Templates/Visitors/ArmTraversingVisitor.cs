
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System.Collections.Generic;

namespace PSArm.Templates.Visitors
{
    public class ArmTravsersingVisitor : IArmVisitor<VisitAction>
    {
        protected virtual VisitAction DefaultVisit(ArmElement element)
        {
            return VisitAction.Continue;
        }

        public virtual void PostVisit(ArmElement element)
        {
            // Do nothing -- for override
        }

        public virtual VisitAction VisitArray(ArmArray array) => DefaultVisit(array);

        public virtual VisitAction VisitBooleanValue(ArmBooleanLiteral booleanValue) => DefaultVisit(booleanValue);

        public virtual VisitAction VisitDoubleValue(ArmDoubleLiteral doubleValue) => DefaultVisit(doubleValue);

        public virtual VisitAction VisitFunctionCall(ArmFunctionCallExpression functionCall) => DefaultVisit(functionCall);

        public virtual VisitAction VisitIndexAccess(ArmIndexAccessExpression indexAccess) => DefaultVisit(indexAccess);

        public virtual VisitAction VisitIntegerValue(ArmIntegerLiteral integerValue) => DefaultVisit(integerValue);

        public virtual VisitAction VisitMemberAccess(ArmMemberAccessExpression memberAccess) => DefaultVisit(memberAccess);

        public virtual VisitAction VisitNestedTemplate(ArmNestedTemplate nestedTemplate) => DefaultVisit(nestedTemplate);

        public virtual VisitAction VisitNullValue(ArmNullLiteral nullValue) => DefaultVisit(nullValue);

        public virtual VisitAction VisitObject(ArmObject obj) => DefaultVisit(obj);

        public virtual VisitAction VisitOutput(ArmOutput output) => DefaultVisit(output);

        public virtual VisitAction VisitParameterDeclaration(ArmParameter parameter) => DefaultVisit(parameter);

        public virtual VisitAction VisitParameterReference(ArmParameterReferenceExpression parameterReference) => DefaultVisit(parameterReference);

        public virtual VisitAction VisitResource(ArmResource resource) => DefaultVisit(resource);

        public virtual VisitAction VisitSku(ArmSku sku) => DefaultVisit(sku);

        public virtual VisitAction VisitStringValue(ArmStringLiteral stringValue) => DefaultVisit(stringValue);

        public virtual VisitAction VisitTemplate(ArmTemplate template) => DefaultVisit(template);

        public virtual VisitAction VisitTemplateResource(ArmTemplateResource templateResource) => DefaultVisit(templateResource);

        public virtual VisitAction VisitVariableDeclaration(ArmVariable variable) => DefaultVisit(variable);

        public virtual VisitAction VisitVariableReference(ArmVariableReferenceExpression variableReference) => DefaultVisit(variableReference);

        private bool ShouldStop(VisitAction currentAction, out VisitAction parentAction)
        {
            switch (currentAction)
            {
                case VisitAction.SkipChildren:
                    parentAction = VisitAction.Continue;
                    return true;

                case VisitAction.Stop:
                    parentAction = VisitAction.Stop;
                    return true;

                // Continue
                default:
                    parentAction = VisitAction.Continue;
                    return false;
            }
        }

        private VisitAction GetFinalParentAction(VisitAction currentAction)
        {
            ShouldStop(currentAction, out VisitAction nextAction);
            return nextAction;
        }

        private bool VisitCollectionAndCheckStop(IReadOnlyList<ArmElement> array, out VisitAction parentAction)
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (ShouldStop(array[i].RunVisit(this), out parentAction))
                {
                    return true;
                }
            }

            parentAction = VisitAction.Continue;
            return false;
        }

        private bool VisitDictionaryAndCheckStop(IReadOnlyDictionary<IArmString, ArmElement> dictionary, out VisitAction parentAction)
        {
            foreach (KeyValuePair<IArmString, ArmElement> entry in dictionary)
            {
                if (ShouldStop(entry.Key.RunVisit(this), out parentAction))
                {
                    return true;
                }

                if (ShouldStop(entry.Value.RunVisit(this), out parentAction))
                {
                    return true;
                }
            }

            parentAction = VisitAction.Continue;
            return false;
        }

        VisitAction IArmVisitor<VisitAction>.VisitArray(ArmArray array)
        {
            if (ShouldStop(VisitArray(array), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitCollectionAndCheckStop(array, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitBooleanValue(ArmBooleanLiteral booleanValue)
        {
            return GetFinalParentAction(VisitBooleanValue(booleanValue));
        }

        VisitAction IArmVisitor<VisitAction>.VisitDoubleValue(ArmDoubleLiteral doubleValue)
        {
            return GetFinalParentAction(VisitDoubleValue(doubleValue));
        }

        VisitAction IArmVisitor<VisitAction>.VisitFunctionCall(ArmFunctionCallExpression functionCall)
        {
            if (ShouldStop(VisitFunctionCall(functionCall), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (ShouldStop(functionCall.Function.RunVisit(this), out parentAction))
            {
                return parentAction;
            }

            if (functionCall.Arguments is not null)
            {
                if (VisitCollectionAndCheckStop(functionCall.Arguments, out parentAction))
                {
                    return parentAction;
                }
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitIndexAccess(ArmIndexAccessExpression indexAccess)
        {
            if (ShouldStop(VisitIndexAccess(indexAccess), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (ShouldStop(indexAccess.InnerExpression.RunVisit(this), out parentAction))
            {
                return parentAction;
            }

            return GetFinalParentAction(indexAccess.Index.RunVisit(this));
        }

        VisitAction IArmVisitor<VisitAction>.VisitIntegerValue(ArmIntegerLiteral integerValue)
        {
            return GetFinalParentAction(VisitIntegerValue(integerValue));
        }

        VisitAction IArmVisitor<VisitAction>.VisitMemberAccess(ArmMemberAccessExpression memberAccess)
        {
            if (ShouldStop(VisitMemberAccess(memberAccess), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (ShouldStop(memberAccess.InnerExpression.RunVisit(this), out parentAction))
            {
                return parentAction;
            }

            return GetFinalParentAction(memberAccess.Member.RunVisit(this));
        }

        VisitAction IArmVisitor<VisitAction>.VisitNestedTemplate(ArmNestedTemplate nestedTemplate)
        {
            if (ShouldStop(VisitNestedTemplate(nestedTemplate), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(nestedTemplate, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitNullValue(ArmNullLiteral nullValue)
        {
            return GetFinalParentAction(VisitNullValue(nullValue));
        }

        VisitAction IArmVisitor<VisitAction>.VisitOutput(ArmOutput output)
        {
            if (ShouldStop(VisitOutput(output), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(output, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitParameterDeclaration(ArmParameter parameter)
        {
            if (ShouldStop(VisitParameterDeclaration(parameter), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(parameter, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitParameterReference(ArmParameterReferenceExpression parameterReference)
        {
            return GetFinalParentAction(VisitParameterReference(parameterReference));
        }

        VisitAction IArmVisitor<VisitAction>.VisitResource(ArmResource resource)
        {
            if (ShouldStop(VisitResource(resource), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(resource, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitSku(ArmSku sku)
        {
            if (ShouldStop(VisitSku(sku), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(sku, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitStringValue(ArmStringLiteral stringValue)
        {
            return GetFinalParentAction(VisitStringValue(stringValue));
        }

        VisitAction IArmVisitor<VisitAction>.VisitTemplate(ArmTemplate template)
        {
            if (ShouldStop(VisitTemplate(template), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(template, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitTemplateResource(ArmTemplateResource templateResource)
        {
            if (ShouldStop(VisitTemplateResource(templateResource), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(templateResource, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }

        VisitAction IArmVisitor<VisitAction>.VisitVariableDeclaration(ArmVariable variable)
        {
            if (ShouldStop(VisitVariableDeclaration(variable), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (ShouldStop(variable.Name.RunVisit(this), out parentAction))
            {
                return parentAction;
            }

            return GetFinalParentAction(variable.Value.RunVisit(this));
        }

        VisitAction IArmVisitor<VisitAction>.VisitVariableReference(ArmVariableReferenceExpression variableReference)
        {
            return GetFinalParentAction(VisitVariableReference(variableReference));
        }

        VisitAction IArmVisitor<VisitAction>.VisitObject(ArmObject obj)
        {
            if (ShouldStop(VisitObject(obj), out VisitAction parentAction))
            {
                return parentAction;
            }

            if (VisitDictionaryAndCheckStop(obj, out parentAction))
            {
                return parentAction;
            }

            return VisitAction.Continue;
        }
    }
}

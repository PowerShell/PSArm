
// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Templates;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System;
using System.Collections.Generic;
using System.IO;

namespace PSArm.Serialization
{
    internal class PSExpressionWritingVisitor : IArmVisitor<object>
    {
        private readonly TextWriter _writer;

        private readonly Stack<bool> _needParensStack;

        private int _defaultValueStackDepth;

        public PSExpressionWritingVisitor(TextWriter textWriter)
        {
            _writer = textWriter;
            _needParensStack = new Stack<bool>();
            _needParensStack.Push(false);
            _defaultValueStackDepth = -1;
        }

        public void EnterParens()
        {
            _needParensStack.Push(_needParensStack.Count != _defaultValueStackDepth);
        }

        public void ExitParens()
        {
            _needParensStack.Pop();
        }

        public void EnterDefaultValue()
        {
            _writer.Write("(");
            _defaultValueStackDepth = _needParensStack.Count;
        }

        public void ExitDefaultValue()
        {
            _writer.Write(")");
            _defaultValueStackDepth = -1;
        }

        public object VisitArray(ArmArray array)
        {
            Write("@(");
            bool needSeparator = false;
            foreach (ArmElement element in array)
            {
                if (needSeparator)
                {
                    Write(",");
                }

                element.RunVisit(this);

                needSeparator = true;
            }
            Write(")");
            return null;
        }

        public object VisitBooleanValue(ArmBooleanLiteral booleanValue)
        {
            if (booleanValue.Value)
            {
                Write("$true");
            }
            else
            {
                Write("$false");
            }
            return null;
        }

        public object VisitDoubleValue(ArmDoubleLiteral doubleValue)
        {
            Write(doubleValue.Value.ToString());
            return null;
        }

        public object VisitFunctionCall(ArmFunctionCallExpression functionCall)
        {
            if (_needParensStack.Peek())
            {
                Write("(");
            }

            Write(functionCall.Function.CoerceToString());

            _needParensStack.Push(true);

            if (functionCall.Arguments != null && functionCall.Arguments.Count > 0)
            {
                foreach (ArmExpression expr in functionCall.Arguments)
                {
                    Write(" ");
                    expr.RunVisit(this);
                }
            }

            _needParensStack.Pop();

            if (_needParensStack.Peek())
            {
                Write(")");
            }

            return null;
        }

        public object VisitIndexAccess(ArmIndexAccessExpression indexAccess)
        {
            _needParensStack.Push(true);

            indexAccess.InnerExpression.RunVisit(this);

            Write("[");
            indexAccess.Index.RunVisit(this);
            Write("]");

            _needParensStack.Pop();

            return null;
        }

        public object VisitIntegerValue(ArmIntegerLiteral integerValue)
        {
            Write(integerValue.Value.ToString());
            return null;
        }

        public object VisitMemberAccess(ArmMemberAccessExpression memberAccess)
        {
            _needParensStack.Push(true);

            memberAccess.InnerExpression.RunVisit(this);

            _needParensStack.Pop();

            Write(".");

            Write(memberAccess.Member.CoerceToString());

            return null;
        }

        public object VisitNestedTemplate(ArmNestedTemplate nestedTemplate) => VisitTemplate(nestedTemplate);

        public object VisitNullValue(ArmNullLiteral nullValue)
        {
            Write("$null");
            return null;
        }

        public object VisitObject(ArmObject obj)
        {
            Write("@{");
            bool needSeparator = false;
            foreach (KeyValuePair<IArmString, ArmElement> entry in obj)
            {
                if (needSeparator)
                {
                    Write(";");
                }

                entry.Key.CoerceToLiteral().RunVisit(this);
                Write("=");
                entry.Value.RunVisit(this);

                needSeparator = true;
            }
            Write("}");

            return null;
        }

        public object VisitOutput(ArmOutput output)
        {
            throw CreateInvalidException(output);
        }

        public object VisitParameterDeclaration(ArmParameter parameter)
        {
            throw CreateInvalidException(parameter);
        }

        public object VisitParameterReference(ArmParameterReferenceExpression parameterReference)
        {
            Write("$");
            Write(parameterReference.ReferenceName.CoerceToString());
            return null;
        }

        public object VisitResource(ArmResource resource)
        {
            throw CreateInvalidException(resource);
        }

        public object VisitSku(ArmSku sku)
        {
            throw CreateInvalidException(sku);
        }

        public object VisitStringValue(ArmStringLiteral stringValue)
        {
            Write("'");
            Write(stringValue.Value.Replace("'", "''"));
            Write("'");
            return null;
        }

        public object VisitTemplate(ArmTemplate template)
        {
            throw CreateInvalidException(template);
        }

        public object VisitTemplateResource(ArmTemplateResource templateResource) => VisitResource(templateResource);

        public object VisitVariableDeclaration(ArmVariable variable)
        {
            throw CreateInvalidException(variable);
        }

        public object VisitVariableReference(ArmVariableReferenceExpression variableReference)
        {
            Write("$");
            Write(variableReference.ReferenceName.CoerceToString());
            return null;
        }

        private Exception CreateInvalidException(object value)
        {
            throw new InvalidOperationException($"The value '{value}' of type '{value.GetType()}' is not supported as a PowerShell expression.");
        }

        private void Write(string value)
        {
            _writer.Write(value);
        }
    }
}

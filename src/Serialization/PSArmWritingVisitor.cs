
// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using PSArm.Schema;
using PSArm.Templates;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using PSArm.Templates.Visitors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PSArm.Serialization
{
    public class PSArmWritingVisitor : IArmVisitor<object>
    {
        public static void WriteToTextWriter(TextWriter textWriter, ArmTemplate template)
        {
            var visitor = new PSArmWritingVisitor(textWriter);
            template.Visit(visitor);
            textWriter.Flush();
        }

        public static string WriteToString(ArmTemplate template)
        {
            using (var stringWriter = new StringWriter())
            {
                WriteToTextWriter(stringWriter, template);
                return stringWriter.ToString();
            }
        }

        public static void WriteToFile(string path, ArmTemplate template)
        {
            using (var writer = new StreamWriter(path))
            {
                WriteToTextWriter(writer, template);
            }
        }

        private const string IndentStr = "  ";

        private static readonly char[] s_armTypeSeparators = new[] { '/' };

        private static readonly IReadOnlyDictionary<ArmStringLiteral, string> s_resourceDefaultParameters = ResourceSchema.DefaultTopLevelProperties
            .Where(p => p != "type" && p != "apiVersion")
            .Select(p => new ArmStringLiteral(p))
            .ToDictionary(p => p, p => p.Value.PascalCase());

        private readonly TextWriter _writer;

        private readonly PSExpressionWritingVisitor _expressionWriter;

        private int _indent;

        public PSArmWritingVisitor(TextWriter writer)
        {
            _writer = writer;
            _expressionWriter = new PSExpressionWritingVisitor(writer);
            _indent = 0;
        }

        public object VisitArray(ArmArray array)
        {
            throw new InvalidOperationException($"Cannot directly visit arrays");
        }

        public object VisitBooleanValue(ArmBooleanLiteral booleanValue)
        {
            booleanValue.Visit(_expressionWriter);
            return null;
        }

        public object VisitDoubleValue(ArmDoubleLiteral doubleValue)
        {
            doubleValue.Visit(_expressionWriter);
            return null;
        }

        public object VisitFunctionCall(ArmFunctionCallExpression functionCall)
        {
            functionCall.Visit(_expressionWriter);
            return null;
        }

        public object VisitIndexAccess(ArmIndexAccessExpression indexAccess)
        {
            indexAccess.Visit(_expressionWriter);
            return null;
        }

        public object VisitIntegerValue(ArmIntegerLiteral integerValue)
        {
            integerValue.Visit(_expressionWriter);
            return null;
        }

        public object VisitMemberAccess(ArmMemberAccessExpression memberAccess)
        {
            memberAccess.Visit(_expressionWriter);
            return null;
        }

        public object VisitNullValue(ArmNullLiteral nullValue)
        {
            nullValue.Visit(_expressionWriter);
            return null;
        }

        public object VisitObject(ArmObject obj)
        {
            OpenBlock();
            bool needSeparator = false;
            foreach (KeyValuePair<IArmString, ArmElement> entry in obj)
            {
                if (needSeparator)
                {
                    WriteLine();
                }

                WriteKeyword(entry.Key);
                Write(" ");
                entry.Value.Visit(this);

                needSeparator = true;
            }
            CloseBlock();

            return null;
        }

        public object VisitOutput(ArmOutput output)
        {
            Write("Output ");
            WriteExpression(output.Name);

            if (output.Type != null)
            {
                Write(" -Type ");
                WriteExpression(output.Type);
            }

            Write(" -Value ");
            WriteExpression(output.Value);

            return null;
        }

        public object VisitParameterDeclaration(ArmParameter parameter)
        {
            WriteAllowedValues(parameter.AllowedValues);
            WriteParameterType(parameter.Type.CoerceToLiteral());
            WriteVariable(parameter.Name.CoerceToString());
            WriteDefaultValue(parameter.DefaultValue);
            return null;
        }

        public object VisitParameterReference(ArmParameterReferenceExpression parameterReference)
        {
            parameterReference.Visit(_expressionWriter);
            return null;
        }

        public object VisitResource(ArmResource resource)
        {
            Write("Resource ");
            WriteExpression(resource.Name);
            string[] typeParts = resource.Type.CoerceToString().Split(s_armTypeSeparators, count: 2);
            Write(" -Provider ");
            WriteString(typeParts[0]);
            Write(" -Type ");
            WriteString(typeParts[1]);
            Write(" -ApiVersion ");
            WriteExpression(resource.ApiVersion);
            foreach (KeyValuePair<ArmStringLiteral, string> defaultParameter in s_resourceDefaultParameters)
            {
                if (resource.TryGetValue(defaultParameter.Key, out ArmElement value))
                {
                    Write($" -{defaultParameter.Value} ");
                    WriteExpression(value);
                }
            }
            Write(" ");

            OpenBlock();

            bool needNewline = false;
            if (resource.Sku != null)
            {
                resource.Sku.Visit(this);
                needNewline = true;
            }

            if (resource.Properties != null && resource.Properties.Count > 0)
            {
                if (needNewline)
                {
                    WriteLine();
                }

                Write("Properties ");
                OpenBlock();

                bool needSeparator = false;
                foreach (KeyValuePair<IArmString, ArmElement> property in resource.Properties)
                {
                    if (needSeparator)
                    {
                        WriteLine();
                    }

                    WritePropertyInvocation(property.Key, property.Value);

                    needSeparator = true;
                }

                CloseBlock();
                needNewline = true;
            }

            if (resource.Resources != null && resource.Resources.Count > 0)
            {
                if (needNewline)
                {
                    WriteLine();
                }

                bool needSeparator = false;
                foreach (KeyValuePair<IArmString, ArmResource> subResource in (IDictionary<IArmString, ArmResource>)resource.Resources)
                {
                    if (needSeparator)
                    {
                        WriteLine();
                    }

                    subResource.Value.Visit(this);
                    needSeparator = true;
                }

                needNewline = true;
            }

            if (resource.DependsOn != null && resource.DependsOn.Count > 0)
            {
                if (needNewline)
                {
                    WriteLine();
                }

                bool needSeparator = false;
                foreach (ArmElement dependsOn in resource.DependsOn)
                {
                    if (needSeparator)
                    {
                        WriteLine();
                    }

                    Write("DependsOn ");
                    WriteExpression(dependsOn);
                }
            }

            CloseBlock();

            return null;
        }

        public object VisitSku(ArmSku sku)
        {
            Write("Sku ");
            WriteExpression(sku.Name);

            if (sku.Capacity != null)
            {
                Write(" -Capacity ");
                WriteExpression(sku.Capacity);
            }

            if (sku.Family != null)
            {
                Write(" -Family ");
                WriteExpression(sku.Family);
            }

            if (sku.Size != null)
            {
                Write(" -Size ");
                WriteExpression(sku.Size);
            }

            if (sku.Tier != null)
            {
                Write(" -Tier ");
                WriteExpression(sku.Tier);
            }

            return null;
        }

        public object VisitStringValue(ArmStringLiteral stringValue)
        {
            stringValue.Visit(_expressionWriter);
            return null;
        }

        public object VisitTemplate(ArmTemplate template)
        {
            _writer.Write("Arm ");
            OpenBlock();

            WriteParametersAndVariables(template.Parameters, template.Variables);
            WriteResources(template.Resources);
            WriteOutputs(template.Outputs);

            CloseBlock();
            return null;
        }

        public object VisitVariableDeclaration(ArmVariable variable)
        {
            Write("[ArmVariable]");
            WriteLine();
            WriteVariable(variable.Name.CoerceToString());
            Write(" = ");
            variable.Value.Visit(_expressionWriter);
            return null;
        }

        public object VisitVariableReference(ArmVariableReferenceExpression variableReference)
        {
            variableReference.Visit(_expressionWriter);
            return null;
        }

        private void WriteParametersAndVariables(
            IReadOnlyDictionary<IArmString, ArmParameter> parameters,
            IReadOnlyDictionary<IArmString, ArmVariable> variables)
        {
            Write("param(");
            Indent();
            WriteLine();

            bool needSeparator = false;

            if (parameters != null && parameters.Count > 0)
            {
                foreach (KeyValuePair<IArmString, ArmParameter> parameter in parameters)
                {
                    if (needSeparator)
                    {
                        Write(",");
                        WriteLine(lineCount: 2);
                    }

                    parameter.Value.Visit(this);
                    needSeparator = true;
                }
            }

            if (variables != null && variables.Count > 0)
            {
                foreach (KeyValuePair<IArmString, ArmVariable> variable in variables)
                {
                    if (needSeparator)
                    {
                        Write(",");
                        WriteLine(lineCount: 2);
                    }

                    variable.Value.Visit(this);
                    needSeparator = true;
                }
            }
        }

        private void WriteAllowedValues(IReadOnlyList<ArmElement> allowedValues)
        {
            if (allowedValues == null
                || allowedValues.Count == 0)
            {
                return;
            }

            Write("[ValidateSet(");
            allowedValues[0].Visit(_expressionWriter);
            for (int i = 1; i < allowedValues.Count; i++)
            {
                Write(", ");
                allowedValues[i].Visit(_expressionWriter);
            }
            Write(")]");
            WriteLine();
        }

        private void WriteParameterType(ArmStringLiteral typeName)
        {
            Write("[ArmParameter[");
            Write(typeName.Value);
            Write("]]");
            WriteLine();
        }

        private void WriteDefaultValue(ArmElement defaultValue)
        {
            if (defaultValue == null)
            {
                return;
            }

            Write(" = ");
            WriteExpression(defaultValue);
        }

        private void WriteResources(IReadOnlyList<ArmResource> resources)
        {
            if (resources == null || resources.Count == 0)
            {
                return;
            }

            bool needSeparator = false;
            foreach (ArmResource resource in resources)
            {
                if (needSeparator)
                {
                    WriteLine();
                }

                resource.Visit(this);
            }
        }

        private void WriteOutputs(IReadOnlyDictionary<IArmString, ArmOutput> outputs)
        {
            if (outputs == null || outputs.Count == 0)
            {
                return;
            }

            bool needSeparator = false;
            foreach (KeyValuePair<IArmString, ArmOutput> output in outputs)
            {
                if (needSeparator)
                {
                    WriteLine();
                }

                output.Value.Visit(this);
                needSeparator = true;
            }
        }

        private void WriteExpression(IArmString value) => WriteExpression((ArmElement)value);

        private void WriteExpression(ArmElement value)
        {
            _expressionWriter.EnterParens();
            value.Visit(_expressionWriter);
            _expressionWriter.ExitParens();
        }

        private void WritePropertyInvocation(IArmString keyword, ArmElement value)
        {
            if (value is ArmArray arrayBody)
            {
                bool needSeparator = false;
                foreach (ArmElement element in arrayBody)
                {
                    if (needSeparator)
                    {
                        WriteLine();
                    }

                    // TODO: Work out how nested arrays work...

                    WritePropertyInvocation(keyword, element);
                    needSeparator = true;
                }

                return;
            }

            WriteKeyword(keyword);
            Write(" ");
            value.Visit(this);
        }

        private void WriteKeyword(IArmString keyword)
        {
            Write(keyword.CoerceToString().PascalCase());
        }

        private void OpenBlock()
        {
            Write("{");
            Indent();
            WriteLine();
        }

        private void CloseBlock()
        {
            Dedent();
            WriteLine();
            Write("}");
        }

        private void WriteString(string s)
        {
            Write("'");
            Write(s.Replace("'", "''"));
            Write("'");
        }

        private void WriteVariable(string variableName)
        {
            Write("$");
            Write(variableName);
        }

        private void WriteLine()
        {
            _writer.WriteLine();
            WriteIndent();
        }

        private void WriteLine(int lineCount)
        {
            for (int i = 0; i < lineCount; i++)
            {
                _writer.WriteLine();
            }

            WriteIndent();
        }

        private void WriteLine(string s)
        {
            _writer.WriteLine(s);
            WriteIndent();
        }

        private void Write(string s)
        {
            _writer.Write(s);
        }

        private void Indent()
        {
            _indent++;
        }

        private void Dedent()
        {
            _indent--;
        }

        private void WriteIndent()
        {
            for (int i = 0; i < _indent; i++)
            {
                _writer.Write(IndentStr);
            }
        }

        public object VisitNestedTemplate(ArmNestedTemplate nestedTemplate) => VisitTemplate(nestedTemplate);

        public object VisitTemplateResource(ArmTemplateResource templateResource) => VisitResource(templateResource);
    }
}

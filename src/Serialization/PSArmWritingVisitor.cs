
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Commands.Template;
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
using System.Text;

namespace PSArm.Serialization
{
    public class PSArmWritingVisitor : IArmVisitor<object>
    {
        public static void WriteToTextWriter(TextWriter textWriter, ArmTemplate template)
        {
            var visitor = new PSArmWritingVisitor(textWriter);
            template.RunVisit(visitor);
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

        public static void WriteToFile(string path, ArmTemplate template) => WriteToFile(path, template, FileMode.Create);

        public static void WriteToFile(string path, ArmTemplate template, FileMode fileMode)
        {
            using (var file = new FileStream(path, fileMode, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(file, Encoding.UTF8))
            {
                WriteToTextWriter(writer, template);
            }
        }

        private const string IndentStr = "  ";

        private static readonly char[] s_armTypeSeparators = new[] { '/' };

        private static readonly IReadOnlyList<string> s_skippedResourceProperties = new string[]
        {
            "type",
            "apiVersion",
            "sku",
            "dependsOn",
        };

        private static readonly IReadOnlyDictionary<ArmStringLiteral, string> s_resourceDefaultParameters = ResourceSchema.DefaultTopLevelProperties
            .Where(p => !s_skippedResourceProperties.Contains(p, StringComparer.OrdinalIgnoreCase))
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
            booleanValue.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitDoubleValue(ArmDoubleLiteral doubleValue)
        {
            doubleValue.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitFunctionCall(ArmFunctionCallExpression functionCall)
        {
            functionCall.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitIndexAccess(ArmIndexAccessExpression indexAccess)
        {
            indexAccess.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitIntegerValue(ArmIntegerLiteral integerValue)
        {
            integerValue.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitMemberAccess(ArmMemberAccessExpression memberAccess)
        {
            memberAccess.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitNullValue(ArmNullLiteral nullValue)
        {
            nullValue.RunVisit(_expressionWriter);
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

                if (entry.Value is ArmArray array)
                {
                    WriteArray(entry.Key, array);
                }
                else
                {
                    WriteKeyword(entry.Key);
                    Write(" ");
                    _expressionWriter.EnterParens();
                    entry.Value.RunVisit(this);
                    _expressionWriter.ExitParens();
                }

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
            parameterReference.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitResource(ArmResource resource)
        {
            Write("Resource ");
            WriteExpression(resource.Name);
            string[] typeParts = resource.Type.CoerceToString().Split(s_armTypeSeparators, count: 2);
            Write($" -{nameof(NewPSArmResourceCommand.Namespace)} ");
            WriteString(typeParts[0]);
            Write($" -{nameof(NewPSArmResourceCommand.Type)} ");
            WriteString(typeParts[1]);
            Write($" -{nameof(NewPSArmResourceCommand.ApiVersion)} ");
            WriteExpression(resource.ApiVersion);
            foreach (KeyValuePair<ArmStringLiteral, string> defaultParameter in s_resourceDefaultParameters)
            {
                if (!defaultParameter.Value.Is("Name")
                    && resource.TryGetValue(defaultParameter.Key, out ArmElement value))
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
                resource.Sku.RunVisit(this);
                needNewline = true;
            }

            if (resource.Properties != null && resource.Properties.Count > 0)
            {
                if (needNewline)
                {
                    WriteLine();
                }

                Write("properties ");
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

                    subResource.Value.RunVisit(this);
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

                Write("DependsOn @(");
                Indent();
                WriteLine();
                bool needSeparator = false;
                foreach (ArmElement dependsOn in resource.DependsOn)
                {
                    if (needSeparator)
                    {
                        WriteLine();
                    }

                    WriteExpression(dependsOn, useParens: false);
                    needSeparator = true;
                }
                Dedent();
                WriteLine();
                Write(")");
            }

            CloseBlock();

            return null;
        }

        public object VisitSku(ArmSku sku)
        {
            Write($"{NewPSArmSkuCommand.KeywordName} ");
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
            stringValue.RunVisit(_expressionWriter);
            return null;
        }

        public object VisitTemplate(ArmTemplate template)
        {
            _writer.Write("Arm ");
            OpenBlock();

            WriteParametersAndVariables(template.Parameters, template.Variables);

            bool needSeparator = false;
            WriteResources(template.Resources, ref needSeparator);
            WriteOutputs(template.Outputs, ref needSeparator);

            CloseBlock();
            return null;
        }

        public object VisitVariableDeclaration(ArmVariable variable)
        {
            Write("[ArmVariable]");
            WriteLine();
            WriteVariable(variable.Name.CoerceToString());
            WriteDefaultValue(variable.Value);
            return null;
        }

        public object VisitVariableReference(ArmVariableReferenceExpression variableReference)
        {
            variableReference.RunVisit(_expressionWriter);
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

                    parameter.Value.RunVisit(this);
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

                    variable.Value.RunVisit(this);
                    needSeparator = true;
                }
            }

            Dedent();
            WriteLine();
            Write(")");
            WriteLine(lineCount: 2);
        }

        private void WriteAllowedValues(IReadOnlyList<ArmElement> allowedValues)
        {
            if (allowedValues == null
                || allowedValues.Count == 0)
            {
                return;
            }

            Write("[ValidateSet(");
            allowedValues[0].RunVisit(_expressionWriter);
            for (int i = 1; i < allowedValues.Count; i++)
            {
                Write(", ");
                allowedValues[i].RunVisit(_expressionWriter);
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
            _expressionWriter.EnterParens();
            WriteExpression(defaultValue);
            _expressionWriter.ExitParens();
        }

        private void WriteResources(IReadOnlyList<ArmResource> resources, ref bool needSeparator)
        {
            if (resources == null || resources.Count == 0)
            {
                return;
            }

            foreach (ArmResource resource in resources)
            {
                if (needSeparator)
                {
                    WriteLine(lineCount: 2);
                }

                resource.RunVisit(this);
                needSeparator = true;
            }
        }

        private void WriteOutputs(IReadOnlyDictionary<IArmString, ArmOutput> outputs, ref bool needSeparator)
        {
            if (outputs == null || outputs.Count == 0)
            {
                return;
            }

            bool first = true;
            foreach (KeyValuePair<IArmString, ArmOutput> output in outputs)
            {
                if (needSeparator)
                {
                    // This groups the outputs nicely
                    WriteLine(lineCount: first ? 2 : 1);
                }

                output.Value.RunVisit(this);
                needSeparator = true;
                first = false;
            }
        }

        private void WriteArray(IArmString key, ArmArray values)
        {
            bool needsSeparator = false;

            foreach (ArmElement element in values)
            {
                if (needsSeparator)
                {
                    WriteLine();
                }

                WriteKeyword(key);
                Write(" ");
                element.RunVisit(this);

                needsSeparator = true;
            }
        }

        private void WriteExpression(IArmString value) => WriteExpression((ArmElement)value);

        private void WriteExpression(ArmElement value, bool useParens = true)
        {
            if (useParens) { _expressionWriter.EnterParens(); }
            value.RunVisit(_expressionWriter);
            if (useParens) { _expressionWriter.ExitParens(); }
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
            value.RunVisit(this);
        }

        private void WriteKeyword(IArmString keyword)
        {
            Write(keyword.CoerceToString());
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

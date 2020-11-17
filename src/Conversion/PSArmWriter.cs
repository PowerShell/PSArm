using Microsoft.PowerShell.Commands;
using PSArm.ArmBuilding;
using PSArm.Expression;
using PSArm.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;

namespace PSArm.Conversion
{
    internal struct PSArmWriter
    {
        public static void WriteToTextWriter(TextWriter textWriter, ArmTemplate template)
        {
            new PSArmWriter(textWriter, template).WriteArmTemplate();
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

        private static readonly char[] s_armTypeSeparators = new[] { '/' };

        private const string IndentStr = "  ";

        private readonly TextWriter _writer;

        private readonly ArmTemplate _template;

        private int _indent;

        public PSArmWriter(TextWriter writer, ArmTemplate template)
        {
            _writer = writer;
            _template = template;
            _indent = 0;
        }

        public void WriteTemplate()
        {
            WriteArmTemplate();
            _writer.Flush();
        }

        private void WriteArmTemplate()
        {
            Write("Arm ");
            OpenBlock();
            WriteParametersAndVariables();
            WriteResources();
            WriteOutputs();
            CloseBlock();
        }

        private void WriteParametersAndVariables()
        {
            Write("param(");
            Indent();
            WriteLine();

            bool wroteParameters = false;
            if (_template.Parameters != null && _template.Parameters.Count > 0)
            {
                wroteParameters = true;
                WriteParameterDeclaration(_template.Parameters[0]);

                for (int i = 1; i < _template.Parameters.Count; i++)
                {
                    Write(",");
                    WriteLine(lineCount: 2);
                    WriteParameterDeclaration(_template.Parameters[i]);
                }
            }

            if (_template.Variables != null && _template.Variables.Count > 0)
            {
                if (wroteParameters)
                {
                    Write(",");
                    WriteLine(lineCount: 2);
                }

                WriteVariableDeclaration(_template.Variables[0]);

                for (int i = 1; i < _template.Variables.Count; i++)
                {
                    Write(",");
                    WriteLine(lineCount: 2);
                    WriteVariableDeclaration(_template.Variables[i]);
                }
            }

            Dedent();
            WriteLine();
            Write(")");
            WriteLine(lineCount: 2);
        }

        private void WriteParameterDeclaration(ArmParameter parameter)
        {
            WriteAllowedValues(parameter.AllowedValues);
            WriteParameterType(parameter.GetType());
            WriteVariable(parameter.Name);
            WriteDefaultValue(parameter.DefaultValue);
        }

        private void WriteAllowedValues(IReadOnlyList<IArmValue> allowedValues)
        {
            if (allowedValues == null
                || allowedValues.Count == 0)
            {
                return;
            }

            Write("[ValidateSet(");
            WriteValue(allowedValues[0]);
            for (int i = 1; i < allowedValues.Count; i++)
            {
                Write(", ");
                WriteValue(allowedValues[i]);
            }
            Write(")]");
            WriteLine();
        }

        private void WriteParameterType(Type parameterType)
        {
            string psType = GetPowerShellTypeFromType(parameterType.GenericTypeArguments[0]);
            Write("[ArmParameter[");
            Write(psType);
            Write("]]");
            WriteLine();
        }

        private void WriteDefaultValue(IArmValue defaultValue)
        {
            if (defaultValue == null)
            {
                return;
            }

            Write(" = ");
            WriteValue(defaultValue, includeParens: true);
        }

        private void WriteVariableDeclaration(ArmVariable variable)
        {
            Write("[ArmVariable]");
            WriteLine();
            WriteVariable(variable.Name);
            Write(" = ");
            WriteValue(variable.Value, includeParens: true);
        }

        private void WriteResources()
        {
            if (_template.Resources == null || _template.Resources.Count == 0)
            {
                return;
            }

            foreach (ArmResource resource in _template.Resources)
            {
                WriteResource(resource);
                WriteLine();
            }
        }

        private void WriteResource(ArmResource resource)
        {
            Write("Resource ");
            WriteValue(resource.Name, includeParens: true);
            string[] typeParts = resource.Type.Split(s_armTypeSeparators, count: 2);
            Write(" -Provider ");
            WriteString(typeParts[0]);
            Write(" -Type ");
            WriteString(typeParts[1]);
            Write(" -ApiVersion ");
            WriteString(resource.ApiVersion);
            if (resource.Location != null)
            {
                Write(" -Location ");
                WriteValue(resource.Location, includeParens: true);
            }
            if (resource.Kind != null)
            {
                Write(" -Kind ");
                WriteValue(resource.Kind);
            }
            Write(" ");
            OpenBlock();

            bool needNewline = false;
            if (resource.Sku != null)
            {
                needNewline = true;
                WriteSku(resource.Sku);
            }

            if (resource.Properties != null && resource.Properties.Count > 0)
            {
                if (needNewline) { WriteLine(); }
                needNewline = true;
                Write("Properties ");
                OpenBlock();

                bool first = true;
                foreach (KeyValuePair<string, ArmPropertyInstance> property in resource.Properties)
                {
                    if (!first)
                    {
                        WriteLine();
                    }
                    WriteProperty(property.Key, property.Value);
                    first = false;
                }

                CloseBlock();
            }

            /*
            if (resource.Subresources != null && resource.Subresources.Count > 0)
            {
                Write("Resources ");
                OpenBlock();

                CloseBlock();
                WriteLine();
            }
            */

            if (resource.DependsOn != null && resource.DependsOn.Count > 0)
            {
                if (needNewline) { WriteLine(); }
                bool first = true;
                foreach (IArmExpression dependsOn in resource.DependsOn)
                {
                    if (!first)
                    {
                        WriteLine();
                    }
                    WriteDependsOn(dependsOn);
                    first = false;
                }
            }

            CloseBlock();
        }

        private void WriteSku(ArmSku sku)
        {
            Write("Sku ");
            WriteValue(sku.Name);

            if (sku.Capacity != null)
            {
                Write(" -Capacity ");
                WriteValue(sku.Capacity, includeParens: true);
            }

            if (sku.Family != null)
            {
                Write(" -Family ");
                WriteValue(sku.Family, includeParens: true);
            }

            if (sku.Size != null)
            {
                Write(" -Size ");
                WriteValue(sku.Size, includeParens: true);
            }

            if (sku.Tier != null)
            {
                Write(" -Tier ");
                WriteValue(sku.Tier, includeParens: true);
            }
        }

        private void WriteProperty(string name, ArmPropertyInstance property)
        {
            switch (property)
            {
                case ArmPropertyValue pValue:
                    WriteProperty(name, pValue);
                    return;

                case ArmPropertyObject pObject:
                    WriteProperty(name, pObject);
                    return;

                case ArmPropertyArray pArray:
                    WriteProperty(name, pArray);
                    return;

                default:
                    throw new ArgumentException($"Unsupported ARM property type '{property.GetType().FullName}'");
            }
        }

        private void WriteProperty(string name, ArmPropertyValue value)
        {
            Write(Pascal(name));
            Write(" ");
            WriteValue(value.Value);
        }

        private void WriteProperty(string name, ArmPropertyObject pObject)
        {
            Write(Pascal(name));
            Write(" ");

            bool hasParameters = pObject.Parameters != null && pObject.Parameters.Count > 0;
            bool hasProperties = pObject.Properties != null && pObject.Properties.Count > 0;

            if (hasParameters && pObject.Parameters.TryGetValue("name", out IArmValue propertyName))
            {
                WriteValue(propertyName);
                Write(" ");
            }

            if (hasParameters || hasProperties)
            {
                OpenBlock();
                if (hasParameters)
                {
                    bool first = true;
                    foreach (KeyValuePair<string, IArmValue> parameter in pObject.Parameters)
                    {
                        if (parameter.Key.Is("name")) { continue; }
                        if (!first) { WriteLine(); }
                        WriteAsProperty(parameter.Key, parameter.Value);
                        first = false;
                    }
                }

                if (hasProperties)
                {
                    bool first = true;
                    foreach (KeyValuePair<string, ArmPropertyInstance> subProperty in pObject.Properties)
                    {
                        if (!first) { WriteLine(); }
                        WriteProperty(subProperty.Key, subProperty.Value);
                        first = false;
                    }
                }
                CloseBlock();
            }
        }

        private void WriteProperty(string name, ArmPropertyArray array)
        {
            string itemName = Depluralize(name);
            foreach (ArmPropertyArrayItem item in array.Items)
            {
                WriteProperty(itemName, item);
                WriteLine();
            }
        }

        private void WriteAsProperty(string name, IArmValue value)
        {
            string cmdName = Pascal(name);
            switch (value)
            {
                case IArmExpression expression:
                    WriteAsProperty(cmdName, expression);
                    return;

                case ArmObject obj:
                    WriteAsProperty(cmdName, obj);
                    return;

                case ArmArray array:
                    WriteAsProperty(Depluralize(cmdName), array);
                    return;
            }
        }

        private void WriteAsProperty(string name, IArmExpression expression)
        {
            Write(name);
            Write(" ");
            WriteValue(expression, includeParens: true);
        }

        private void WriteAsProperty(string name, ArmObject obj)
        {
            Write(name);
            Write(" ");

            OpenBlock();
            bool first = true;
            foreach (KeyValuePair<string, IArmValue> entry in obj)
            {
                if (!first) { WriteLine(); }
                WriteAsProperty(entry.Key, entry.Value);
                first = false;
            }
            CloseBlock();
        }

        private void WriteAsProperty(string name, ArmArray array)
        {
            bool first = true;
            foreach (IArmValue item in array)
            {
                if (!first) { WriteLine(); }
                WriteAsProperty(name, item);
                first = false;
            }
        }

        private void WriteDependsOn(IArmExpression value)
        {
            Write("DependsOn ");
            WriteValue(value, includeParens: true);
        }

        private void WriteOutputs()
        {
            if (_template.Outputs == null || _template.Outputs.Count == 0)
            {
                return;
            }

            foreach (ArmOutput output in _template.Outputs)
            {
                WriteOutput(output);
                WriteLine();
            }
        }

        private void WriteOutput(ArmOutput output)
        {
            Write("Output ");
            WriteValue(output.Name, includeParens: true);

            if (output.Type != null)
            {
                Write(" -Type ");
                WriteValue(output.Type, includeParens: true);
            }

            Write(" -Value ");
            WriteValue(output.Value, includeParens: true);
        }

        private void WriteVariable(string variableName)
        {
            Write("$");
            Write(variableName);
        }

        private void WriteString(string value)
        {
            Write("'");
            Write(value.Replace("'", "''"));
            Write("'");
        }

        private void WriteInteger(long value)
        {
            Write(value.ToString());
        }

        private void WriteBoolean(bool value)
        {
            if (value)
            {
                Write("$true");
            }
            else
            {
                Write("$false");
            }
        }

        private void WriteValue(IArmValue value, bool includeParens = false)
        {
            switch (value)
            {
                case IArmExpression expression:
                    WriteValue(expression, includeParens);
                    break;

                case ArmObject armObject:
                    WriteValue(armObject);
                    break;

                case ArmArray armArray:
                    WriteValue(armArray);
                    break;

                default:
                    throw new ArgumentException($"Argument '{nameof(value)}' is of unsupported value type '{value.GetType().FullName}'");
            }
        }

        private void WriteValue(ArmObject armObject)
        {
            Write("@{");
            bool first = true;
            foreach (KeyValuePair<string, IArmValue> entry in armObject)
            {
                if (!first) { Write(";");  }
                WriteString(entry.Key);
                Write(" = ");
                WriteValue(entry.Value);
                first = false;
            }
            Write("}");
        }

        private void WriteValue(ArmArray armArray)
        {
            Write("@(");
            bool first = true;
            foreach (IArmValue item in armArray)
            {
                if (!first) { Write(","); }
                WriteValue(item);
                first = false;
            }
            Write(")");
        }

        private void WriteValue(IArmExpression expression, bool includeParens)
        {
            switch (expression)
            {
                case ArmParameter parameter:
                    WriteValue(parameter);
                    return;

                case ArmVariable variable:
                    WriteValue(variable);
                    return;

                case ArmLiteral literal:
                    WriteValue(literal);
                    return;

                case ArmFunctionCall call:
                    WriteValue(call, includeParens);
                    return;

                case ArmMemberAccess memberAccess:
                    WriteValue(memberAccess);
                    return;

                case ArmIndexAccess indexAccess:
                    WriteValue(indexAccess);
                    return;

                default:
                    throw new ArgumentException($"Cannot convert unsupported ARM expression of type: '{expression.GetType().FullName}'");
            }
        }

        private void WriteValue(ArmParameter parameter)
        {
            WriteVariable(parameter.Name);
        }

        private void WriteValue(ArmVariable variable)
        {
            WriteVariable(variable.Name);
        }

        private void WriteValue(ArmLiteral literal)
        {
            switch (literal)
            {
                case ArmStringLiteral str:
                    WriteValue(str);
                    return;

                case ArmIntLiteral integer:
                    WriteValue(integer);
                    return;

                case ArmBoolLiteral boolean:
                    WriteValue(boolean);
                    return;

                default:
                    throw new ArgumentException($"Cannot convert unsupported ARM literal type: '{literal.GetType().FullName}'");
            }
        }

        private void WriteValue(ArmStringLiteral str)
        {
            WriteString(str.Value);
        }

        private void WriteValue(ArmIntLiteral integer)
        {
            WriteInteger(integer.Value);
        }

        private void WriteValue(ArmBoolLiteral boolean)
        {
            WriteBoolean(boolean.Value);
        }

        private void WriteValue(ArmFunctionCall call, bool includeParens = false)
        {
            if (call.FunctionName.Is("parameters")
                || call.FunctionName.Is("variables"))
            {
                WriteVariable(((ArmStringLiteral)call.Arguments[0]).Value);
                return;
            }

            if (includeParens)
            {
                Write("(");
            }

            Write(call.FunctionName);

            if (call.Arguments == null || call.Arguments.Length == 0)
            {
                if (includeParens)
                {
                    Write(")");
                }
                return;
            }

            foreach (IArmExpression value in call.Arguments)
            {
                Write(" ");
                WriteValue(value, includeParens: true);
            }

            if (includeParens)
            {
                Write(")");
            }
        }

        private void WriteValue(ArmMemberAccess memberAccess)
        {
            WriteValue(memberAccess.Expression, includeParens: true);
            Write(".");
            Write(memberAccess.Member);
        }

        private void WriteValue(ArmIndexAccess indexAccess)
        {
            WriteValue(indexAccess.Expression, includeParens: true);
            Write("[");
            WriteInteger(indexAccess.Index);
            Write("]");
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

        private string GetPowerShellTypeFromType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return "string";

                case TypeCode.Boolean:
                    return "bool";

                case TypeCode.Int32:
                    return "int";

                /*
                // Not yet supported

                case TypeCode.Int64:
                    return "long";

                case TypeCode.Decimal:
                    return "decimal";

                case TypeCode.Single:
                case TypeCode.Double:
                    return "double";
                */
            }

            if (type == typeof(object))
            {
                return "object";
            }

            if (type == typeof(SecureString))
            {
                return "securestring";
            }

            if (type == typeof(SecureObject))
            {
                return "secureObject";
            }

            if (type == typeof(Array))
            {
                return "array";
            }

            throw new ArgumentException($"Type '{type}' is not a supported PSArm parameter type");
        }

        private string Pascal(string s)
        {
            return char.IsLower(s[0])
                ? char.ToUpper(s[0]) + s.Substring(1)
                : s;
        }

        private string Depluralize(string s)
        {
            int lastIdx = s.Length - 1;

            if (s[lastIdx] != 's')
            {
                return s;
            }

            return s.Substring(0, lastIdx);
        }
    }
}

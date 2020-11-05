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
            WriteParameters();
            WriteResources();
            WriteOutputs();
            CloseBlock();
        }

        private void WriteParameters()
        {
            if (_template.Parameters == null || _template.Parameters.Count == 0)
            {
                return;
            }

            Write("param(");
            Indent();
            WriteLine();

            WriteParameter(_template.Parameters[0]);

            for (int i = 1; i < _template.Parameters.Count; i++)
            {
                Write(",");
                WriteLine();
                WriteParameter(_template.Parameters[i]);
            }

            Dedent();
            WriteLine();
            Write(")");
            WriteLine(lineCount: 2);
        }

        private void WriteParameter(ArmParameter parameter)
        {
            WriteAllowedValues(parameter.AllowedValues);
            WriteLine();
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

            WriteLine();
            Write("[ValidateSet(");
            WriteValue(allowedValues[0]);
            for (int i = 1; i < allowedValues.Count; i++)
            {
                Write(", ");
                WriteValue(allowedValues[i]);
            }
            Write(")]");
        }

        private void WriteParameterType(Type parameterType)
        {
            string psType = GetPowerShellTypeFromType(parameterType.GenericTypeArguments[0]);
            Write("[ArmParameter[");
            Write(psType);
            Write("]]");
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
            Write(" -Location ");
            WriteValue(resource.Location, includeParens: true);
            if (resource.Kind != null)
            {
                Write(" -Kind ");
                WriteValue(resource.Kind);
            }
            Write(" ");
            OpenBlock();

            if (resource.Sku != null)
            {
                WriteSku(resource.Sku);
                WriteLine();
            }

            if (resource.Properties != null && resource.Properties.Count > 0)
            {
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
                WriteLine();
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

            if (pObject.Parameters != null && pObject.Parameters.Count > 0)
            {
                foreach (KeyValuePair<string, IArmValue> parameter in pObject.Parameters)
                {
                    Write("-");
                    Write(Pascal(parameter.Key));
                    Write(" ");
                    WriteValue(parameter.Value, includeParens: true);
                    Write(" ");
                }
            }

            if (pObject.Properties != null && pObject.Properties.Count > 0)
            {
                OpenBlock();
                bool first = true;
                foreach (KeyValuePair<string, ArmPropertyInstance> subProperty in pObject.Properties)
                {
                    if (!first)
                    {
                        WriteLine();
                    }
                    WriteProperty(subProperty.Key, subProperty.Value);
                    first = false;
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

        private void WriteValue(IArmValue value, bool includeParens = false)
        {
            Write(ConvertFromValue(value, includeParens));
        }

        private string ConvertFromValue(IArmValue armValue, bool includeParens = false)
        {
            var sb = new StringBuilder();
            ConvertFromValue(sb, armValue, includeParens);
            return sb.ToString();
        }

        private void ConvertFromValue(StringBuilder sb, IArmValue armValue, bool includeParens = false)
        {
            switch (armValue)
            {
                case IArmExpression expression:
                    ConvertFromValue(sb, expression, includeParens);
                    break;

                case ArmObject armObject:
                    ConvertFromValue(sb, armObject);
                    break;

                case ArmArray armArray:
                    ConvertFromValue(sb, armArray);
                    break;

                default:
                    throw new ArgumentException($"Argument '{nameof(armValue)}' is of unsupported value type '{armValue.GetType().FullName}'");
            }
        }

        private void ConvertFromValue(StringBuilder sb, ArmObject armObject)
        {
            sb.Append("@{");

            bool first = true;
            foreach (KeyValuePair<string, IArmValue> entry in armObject)
            {
                if (!first)
                {
                    sb.Append("; ");
                }

                sb.Append("'").Append(entry.Key.Replace("'", "''")).Append("'")
                    .Append(" = ")
                    .Append(ConvertFromValue(entry.Value));

                first = false;
            }

            sb.Append("}");
        }

        private void ConvertFromValue(StringBuilder sb, ArmArray armArray)
        {
            sb.Append("@(");
            
            if (armArray.Count == 0)
            {
                sb.Append(")");
                return;
            }

            sb.Append(ConvertFromValue(armArray[0]));

            for (int i = 1; i < armArray.Count; i++)
            {
                sb.Append(", ");
                sb.Append(ConvertFromValue(armArray[i]));
            }

            sb.Append(")");
        }

        private void ConvertFromValue(StringBuilder sb, IArmExpression armExpression, bool includeParens = false)
        {
            switch (armExpression)
            {
                case ArmParameter parameter:
                    sb.Append("$").Append(parameter.Name);
                    return;

                case ArmVariable variable:
                    sb.Append("$").Append(variable.Name);
                    return;

                case ArmLiteral literal:
                    ConvertFromValue(sb, literal);
                    return;

                case ArmFunctionCall call:
                    ConvertFromValue(sb, call, includeParens);
                    return;

                case ArmMemberAccess memberAccess:
                    ConvertFromValue(sb, memberAccess);
                    return;

                case ArmIndexAccess indexAccess:
                    ConvertFromValue(sb, indexAccess);
                    return;

                default:
                    throw new ArgumentException($"Cannot convert unsupported ARM expression of type: '{armExpression.GetType().FullName}'");
            }
        }

        private void ConvertFromValue(StringBuilder sb, ArmFunctionCall call, bool includeParens = false)
        {
            if (call.FunctionName.Is("parameters")
                || call.FunctionName.Is("variables"))
            {
                sb.Append("$").Append(((ArmStringLiteral)call.Arguments[0]).Value);
                return;
            }

            if (includeParens)
            {
                sb.Append("(");
            }

            sb.Append(call.FunctionName);

            if (call.Arguments == null || call.Arguments.Length == 0)
            {
                if (includeParens)
                {
                    sb.Append(")");
                }
                return;
            }

            foreach (IArmExpression value in call.Arguments)
            {
                sb.Append(" ");
                ConvertFromValue(sb, value, includeParens: true);
            }

            if (includeParens)
            {
                sb.Append(")");
            }
        }

        private void ConvertFromValue(StringBuilder sb, ArmMemberAccess memberAccess)
        {
            ConvertFromValue(sb, memberAccess.Expression, includeParens: true);
            sb.Append(".").Append(memberAccess.Member);
        }

        private void ConvertFromValue(StringBuilder sb, ArmIndexAccess indexAccess)
        {
            ConvertFromValue(sb, indexAccess.Expression, includeParens: true);
            sb.Append("[").Append(indexAccess.Index).Append("]");
        }

        private void ConvertFromValue(StringBuilder sb, ArmLiteral literal)
        {
            switch (literal)
            {
                case ArmStringLiteral str:
                    sb.Append("'").Append(str.Value).Append("'");
                    return;

                case ArmIntLiteral intVal:
                    sb.Append(intVal.Value);
                    return;

                case ArmBoolLiteral boolVal:
                    if (boolVal.Value)
                    {
                        sb.Append("$true");
                    }
                    else
                    {
                        sb.Append("$false");
                    }
                    return;

                default:
                    throw new ArgumentException($"Cannot convert unsupported ARM literal type: '{literal.GetType().FullName}'");
            }
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

        private PSArmWriter Write(string s)
        {
            _writer.Write(s);
            return this;
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

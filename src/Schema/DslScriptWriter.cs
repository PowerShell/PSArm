
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema
{
    /// <summary>
    /// Turns an ARM DSL schema description into a set of PowerShell functions,
    /// implementing the resource DSL based on its schema description.
    /// </summary>
    public class DslScriptWriter : IDslSchemaVisitor
    {
        private readonly StringBuilder _sb;

        private int _indent = 0;

        /// <summary>
        /// Create a new DSL script writer.
        /// </summary>
        public DslScriptWriter()
        {
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Write PowerShell ARM resource DSL script definitions to strings,
        /// keyed by ARM resource type names.
        /// </summary>
        /// <param name="schema">The ARM resource DSL schema to write out to script.</param>
        /// <returns>The script implementations of the given schema, keyed by resource type name.</returns>
        public Dictionary<string, string> WriteDslDefinitions(DslSchema schema)
        {
            var dict = new Dictionary<string, string>();
            foreach (KeyValuePair<string, Dictionary<string, DslSchemaItem>> entry in schema.Subschemas)
            {
                string schemaName = $"{schema.Name}/{entry.Key}";

                if (entry.Value.Count == 0)
                {
                    dict[schemaName] = string.Empty;
                    continue;
                }

                foreach (KeyValuePair<string, DslSchemaItem> topKeyword in entry.Value)
                {
                    Reset();
                    topKeyword.Value.Visit(topKeyword.Key, this);
                    dict[schemaName] = _sb.ToString();
                }
            }
            return dict;
        }

        /// <summary>
        /// Reset the state of the visitor so that it can be reused.
        /// </summary>
        public void Reset()
        {
            _sb.Clear();
            _indent = 0;
        }

        public void VisitCommandKeyword(string commandName, DslCommandSchema command)
        {
            WriteFunctionBeginning(commandName, command.Parameters);

            _sb.Append("Value ");
            WriteLiteral(UnPascal(commandName));
            _sb.Append(' ');
            WriteVariable(command.Parameters[0].Name);

            WriteFunctionEnd();
        }

        public void VisitBodyCommandKeyword(string commandName, DslBodyCommandSchema bodyCommand)
        {
            WriteFunctionBeginning(commandName, bodyCommand.Parameters);

            _sb.Append("Composite ");
            WriteLiteral(UnPascal(commandName));
            _sb.Append(" $PSBoundParameters");

            WriteFunctionEnd();
        }

        public void VisitArrayKeyword(string commandName, DslArraySchema array)
        {
            WriteFunctionBeginning(commandName, array.Parameters, writeBodyParameter: array.Body != null);

            if (array.Body != null)
            {
                foreach (KeyValuePair<string, DslSchemaItem> subSchema in array.Body)
                {
                    subSchema.Value.Visit(subSchema.Key, this);
                    Newline();
                }
            }

            _sb.Append("ArrayItem ");
            WriteLiteral(UnPascal(commandName));
            _sb.Append(" $PSBoundParameters $Body");

            WriteFunctionEnd();
        }

        public void VisitBlockKeyword(string commandName, DslBlockSchema block)
        {
            WriteFunctionBeginning(commandName, block.Parameters, writeBodyParameter: true);

            foreach (KeyValuePair<string, DslSchemaItem> subSchema in block.Body)
            {
                subSchema.Value.Visit(subSchema.Key, this);
                Newline();
            }

            _sb.Append("Block ");
            WriteLiteral(UnPascal(commandName));
            _sb.Append(" $PSBoundParameters $Body");

            WriteFunctionEnd();
        }

        private void WriteFunctionBeginning(string functionName, IReadOnlyList<DslParameter> parameters, bool writeBodyParameter = false)
        {
            _sb.Append("function ").Append(functionName);

            StartBlock();

            _sb.Append("[CmdletBinding()]");
            Newline();
            _sb.Append("param(");
            Indent();
            Newline();

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    DslParameter parameter = parameters[i];
                    WriteParameter(parameter, position: i);

                    if (i < parameters.Count - 1)
                    {
                        _sb.Append(',');
                        Newline();
                        Newline();
                    }
                    else if (writeBodyParameter)
                    {
                        _sb.Append(',');
                        Newline();
                        Newline();
                        WriteParameter("Body", "scriptblock", position: i + 1, validationSet: null);
                    }
                }
            }

            Dedent();
            Newline();
            _sb.Append(')');
            Newline();
            Newline();
        }

        private void WriteParameter(DslParameter parameter, int position)
        {
            WriteParameter(parameter.Name, parameter.Type, position, parameter.Enum);
        }

        private void WriteParameter(string name, string type, int position, IReadOnlyList<object> validationSet)
        {
            _sb.Append("[Parameter(Position = ").Append(position).Append(", Mandatory)]");
            Newline();
            WriteVariable(name);
        }

        private void WriteFunctionEnd()
        {
            EndBlock();
            Newline();
        }

        private void WriteLiteral(object value)
        {
            switch (value)
            {
                case string s:
                    _sb.Append('\'').Append(s.Replace("'", "''")).Append('\'');
                    return;

                case bool b:
                    _sb.Append(b ? "$true" : "$false");
                    return;

                case int i:
                    _sb.Append(i);
                    return;

                case long l:
                    _sb.Append(l);
                    return;

                case double d:
                    _sb.Append(d);
                    return;

                default:
                    throw new NotImplementedException();
            }
        }

        private void WriteVariable(string variableName)
        {
            _sb.Append('$').Append(variableName);
        }

        private string UnPascal(string s)
        {
            return char.IsUpper(s[0])
                ? char.ToLower(s[0]) + s.Substring(1)
                : s;
        }

        private string Pluralise(string s)
        {
            return s + "s";
        }

        private void Indent()
        {
            _indent++;
        }

        private void Dedent()
        {
            _indent--;
        }

        private void Newline()
        {
            _sb.Append('\n').Append(' ', 4 * _indent);
        }

        private void StartBlock()
        {
            Newline();
            _sb.Append('{');
            Indent();
            Newline();
        }

        private void EndBlock()
        {
            Dedent();
            Newline();
            _sb.Append('}');
        }
    }
}

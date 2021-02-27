
// Copyright (c) Microsoft Corporation.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PSArm.Serialization
{
    public class PowerShellWriter
    {
        private const string IndentSpace = "    ";

        private readonly TextWriter _writer;

        private int _indent;

        public PowerShellWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public PowerShellWriter WriteCommand(string commandName)
        {
            return Write(commandName);
        }

        public PowerShellWriter WriteParameter(string parameterName)
        {
            return Write(" -")
                .Write(parameterName);
        }

        public PowerShellWriter WriteVariable(string variableName)
        {
            return Write("$")
                .Write(variableName);
        }

        public PowerShellWriter WriteNull()
        {
            return WriteVariable("null");
        }

        public PowerShellWriter WriteValue(bool value)
        {
            return value
                ? WriteVariable("true")
                : WriteVariable("false");
        }

        public PowerShellWriter WriteValue(string value)
        {
            return Write("'")
                .Write(value.Replace("'", "''"))
                .Write("'");
        }

        public PowerShellWriter WriteValue(int value)
        {
            return Write(value.ToString());
        }

        public PowerShellWriter WriteType(string typeName) => WriteType(typeName, genericArgs: null);

        public PowerShellWriter WriteType(
            string typeName,
            IReadOnlyList<string> genericArgs)
        {
            return Write("[")
                .Write(typeName)
                .Intersperse(
                    (ga) => Write(ga),
                    () => Write(", "),
                    genericArgs)
                .Write("]");
        }

        public PowerShellWriter WriteSpace() => Write(" ");

        public PowerShellWriter Intersperse<T>(
            Action<T> writeElement,
            Action writeSeparator,
            IReadOnlyCollection<T> elements)
        {
            if (elements is null
                || elements.Count == 0)
            {
                return this;
            }

            bool needsSeparator = false;
            foreach (T element in elements)
            {
                if (needsSeparator)
                {
                    writeSeparator();
                }

                writeElement(element);

                needsSeparator = true;
            }

            return this;
        }

        public PowerShellWriter Intersperse(
            Action<string> writeElement,
            string separator,
            IReadOnlyCollection<string> elements)
        {
            if (elements is null
                || elements.Count == 0)
            {
                return this;
            }

            bool needsSeparator = false;
            foreach (string element in elements)
            {
                if (needsSeparator)
                {
                    Write(separator);
                }

                writeElement(element);

                needsSeparator = true;
            }

            return this;
        }

        public PowerShellWriter OpenFunction(string functionName)
        {
            return Write("function ")
                .Write(functionName)
                .OpenBlock();
        }

        public PowerShellWriter CloseFunction()
        {
            return CloseBlock();
        }

        public PowerShellWriter OpenParamBlock()
        {
            return Write("param(")
                .Indent()
                .WriteLine();
        }

        public PowerShellWriter CloseParamBlock()
        {
            return Dedent()
                .WriteLine()
                .Write(")")
                .WriteLine();
        }

        public PowerShellWriter OpenAttribute(string attributeName)
        {
            return Write("[")
                .Write(attributeName)
                .Write("(");
        }

        public PowerShellWriter CloseAttribute()
        {
            return Write(")]");
        }

        public PowerShellWriter OpenBlock()
        {
            return WriteLine()
                .Write("{")
                .Indent()
                .WriteLine();
        }

        public PowerShellWriter CloseBlock()
        {
            return Dedent()
                .WriteLine()
                .Write("}")
                .WriteLine();
        }

        public PowerShellWriter WriteLine(int lineCount = 1)
        {
            for (int i = 0; i < lineCount; i++)
            {
                _writer.WriteLine();
            }

            for (int i = 0; i < _indent; i++)
            {
                _writer.Write(IndentSpace);
            }

            return this;
        }

        public PowerShellWriter Indent()
        {
            _indent++;
            return this;
        }

        public PowerShellWriter Dedent()
        {
            _indent--;
            return this;
        }

        public PowerShellWriter Write(string value)
        {
            _writer.Write(value);
            return this;
        }
    }
}

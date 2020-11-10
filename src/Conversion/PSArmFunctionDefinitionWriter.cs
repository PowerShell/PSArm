using PSArm.Expression;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PSArm.Conversion
{
    public struct PSArmFunctionDefinitionWriter
    {
        public static string WriteToString(IEnumerable<ArmBuiltinFunction> functions)
        {
            using (var stringWriter = new StringWriter())
            {
                new PSArmFunctionDefinitionWriter(stringWriter).WriteFunctions(functions);
                return stringWriter.ToString();
            }
        }

        public static void WriteToFile(string path, IEnumerable<ArmBuiltinFunction> functions)
        {
            using (var fileWriter = new StreamWriter(path))
            {
                new PSArmFunctionDefinitionWriter(fileWriter).WriteFunctions(functions);
            }
        }

        private readonly TextWriter _writer;

        private int _indent;

        public PSArmFunctionDefinitionWriter(TextWriter writer)
        {
            _writer = writer;
            _indent = 0;
        }

        public void WriteFunctions(IEnumerable<ArmBuiltinFunction> functions)
        {
            bool first = true;
            foreach (ArmBuiltinFunction function in functions)
            {
                if (!first)
                {
                    _writer.WriteLine();
                }

                WriteFunction(function);
                first = false;
            }
        }

        private void WriteFunction(ArmBuiltinFunction function)
        {
            Write("function ");
            Write(function.Name);
            OpenBlock();

            WriteParameterCheck(function.MinimumArguments, function.MaximumArguments);
            Write("Call ");
            Write(function.Name);
            Write(" -Arguments ");
            Write("$args");

            CloseBlock();
        }

        private void WriteParameterCheck(int minimumParameters, int? maximumParameters)
        {
            if (minimumParameters > 0)
            {
                Write("if ($args.Count -lt ");
                Write(minimumParameters.ToString());
                Write("){ throw 'Not enough parameters provided' }");
                WriteLine();
            }

            if (maximumParameters != null)
            {
                Write("if ($args.Count -gt ");
                Write(maximumParameters.ToString());
                Write("){ throw 'Exceeded maximum parameter count' }");
                WriteLine();
            }
        }

        private void Write(string value)
        {
            _writer.Write(value);
        }

        private void OpenBlock()
        {
            WriteLine();
            Write("{");
            Indent();
            WriteLine();
        }

        private void CloseBlock()
        {
            Dedent();
            WriteLine();
            Write("}");
            WriteLine();
        }

        private void WriteLine()
        {
            _writer.WriteLine();
            for (int i = 0; i < _indent; i++)
            {
                Write("   ");
            }
        }

        private void Indent()
        {
            _indent++;
        }

        private void Dedent()
        {
            _indent--;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSArm
{
    public class DslCompleter
    {
        public IReadOnlyList<CommandCompletion> CompleteInput(string input, int cursorIndex, Hashtable options)
        {
            Ast ast = Parser.ParseInput(input, out Token[] tokens, out ParseError[] errors);
        }

        public IReadOnlyList<CommandCompletion> CompleteInput(
            Ast ast,
            IReadOnlyList<Token> tokens,
            IScriptPosition cursorPosition,
            Hashtable options)
        {

        }
    }
}
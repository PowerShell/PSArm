using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

namespace PsArm
{
    public static class ArmDslCompletions
    {
        public static CommandCompletion CompleteAnyInput(string input, int cursorColumn, Hashtable options)
        {
            Tuple<Ast, Token[], IScriptPosition> parsedInput = CommandCompletion.MapStringInputToParsedInput(input, cursorColumn);

            if (TryCompleteInput(parsedInput.Item1, parsedInput.Item2, parsedInput.Item3, options, out CommandCompletion completion))
            {
                return completion;
            }

            return CommandCompletion.CompleteInput(parsedInput.Item1, parsedInput.Item2, parsedInput.Item3, options);
        }

        public static CommandCompletion CompleteAnyInput(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options)
        {
            if (TryCompleteInput(ast, tokens, cursorPosition, options, out CommandCompletion completion))
            {
                return completion;
            }

            return CommandCompletion.CompleteInput(ast, tokens, cursorPosition, options);
        }

        public static bool TryCompleteInput(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options, out CommandCompletion completion)
        {
            using (var pwsh = PowerShell.Create())
            {
                CompletionContext context = GetCompletionContext(ast, tokens, cursorPosition, options, pwsh);

                completion = null;
                return false;
            }
        }

        private static CompletionContext GetCompletionContext(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options, PowerShell pwsh)
        {
            Token tokenAtCursor = GetInterestingTokenAtCursor(tokens, cursorPosition, out int index);

            return new CompletionContext()
            {
                AstsOverCursor = ast.FindAll(subAst => ContainsPosition(subAst.Extent, cursorPosition), searchNestedScriptBlocks: true).ToList(),
                Options = options,
                TokenAtCursor = tokenAtCursor,
                TokenBeforeCursor = GetInterestingTokenBeforeCursor(tokens, index, cursorPosition),
                SessionState = pwsh.Runspace.SessionStateProxy,
            };
        }

        private static Token GetInterestingTokenAtCursor(IReadOnlyList<Token> tokens, IScriptPosition cursor, out int tokenIndex)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                if (ContainsPosition(token.Extent, cursor))
                {
                    tokenIndex = i;
                    return token;
                }
            }

            tokenIndex = -1;
            return null;
        }

        private static Token GetInterestingTokenBeforeCursor(IReadOnlyList<Token> tokens, int interestingTokenAfterIndex, IScriptPosition cursor)
        {
            if (interestingTokenAfterIndex > 0)
            {
                return tokens[interestingTokenAfterIndex];
            }

            return null;
        }

        private static bool ContainsPosition(IScriptExtent extent, IScriptPosition position)
        {
            return position.Offset > extent.StartOffset
                && position.Offset <= extent.EndOffset;
        }

        private class CompletionContext
        {
            public Token TokenAtCursor { get; set; }

            public Token TokenBeforeCursor { get; set; }

            public IReadOnlyList<Ast> AstsOverCursor { get; set; }

            public Hashtable Options { get; set; }

            public SessionStateProxy SessionState { get; set; }
        }
    }
}
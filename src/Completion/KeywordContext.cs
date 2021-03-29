
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Language;

namespace PSArm.Completion
{
    /// <summary>
    /// Describes all the information needed about a cursor position in a script
    /// to provide ARM DSL keyword completions at that position.
    /// </summary>
    internal sealed class KeywordContext
    {
        public static KeywordContext BuildFromInput(
            Ast ast,
            IReadOnlyList<Token> tokens,
            IScriptPosition cursorPosition)
        {
            // Now find the AST we're in
            CompletionPositionContext positionContext = GetEffectiveScriptPosition(cursorPosition, tokens);

            Ast containingAst = FindAstFromPositionVisitor.GetContainingAstOfPosition(ast, positionContext.LastNonNewlineToken.Extent.EndScriptPosition);

            if (containingAst is null)
            {
                return null;
            }

            // Find the command AST that we're in
            CommandAst containingCommandAst = GetFirstParentCommandAst(containingAst);

            var context = new KeywordContext
            {
                ContainingAst = containingAst,
                ContainingCommandAst = containingCommandAst,
                FullAst = ast,
                LastTokenIndex = positionContext.LastTokenIndex,
                LastToken = positionContext.LastToken,
                LastNonNewlineToken = positionContext.LastNonNewlineToken,
                EffectivePositionToken = positionContext.EffectivePositionToken,
                Tokens = tokens,
                Position = cursorPosition
            };

            // Build a list of the keyword ASTs we're in going up
            Ast currAst = containingAst;
            var commandAsts = new List<CommandAst>();
            do
            {
                if (currAst is CommandAst commandAst)
                {
                    commandAsts.Add(commandAst);
                }

                currAst = currAst.Parent;
            } while (currAst != null);

            // Then build the context list going back down
            var keywordStack = new List<KeywordContextFrame>(commandAsts.Count);
            int frameIndex = 0;
            for (int i = commandAsts.Count - 1; i >= 0; i--)
            {
                keywordStack.Add(new KeywordContextFrame(context, frameIndex, commandAsts[i]));
                frameIndex++;
            }
            context.KeywordStack = keywordStack;

            return context;
        }

        private KeywordContext()
        {
        }

        /// <summary>
        /// The DSL keywords by scope, from the bottom up.
        /// </summary>
        public List<KeywordContextFrame> KeywordStack { get; private set; }

        /// <summary>
        /// The smallest AST containing our effetive position.
        /// </summary>
        public Ast ContainingAst { get; private set; }

        /// <summary>
        /// The complete AST of the input script we're completing in.
        /// </summary>
        public Ast FullAst { get; private set; }

        /// <summary>
        /// The complete list of tokens of the input script we're in.
        /// </summary>
        public IReadOnlyList<Token> Tokens { get; private set; }

        /// <summary>
        /// The index in the token list of the last token before the cursor.
        /// </summary>
        public int LastTokenIndex { get; private set; }

        /// <summary>
        /// The last token before the cursor.
        /// </summary>
        public Token LastToken { get; private set; }

        /// <summary>
        /// The last non-newline token before the cursor.
        /// </summary>
        public Token LastNonNewlineToken { get; private set; }

        public Token EffectivePositionToken { get; private set; }

        /// <summary>
        /// The position of the cursor.
        /// </summary>
        public IScriptPosition Position { get; private set; }

        /// <summary>
        /// The smallest containing command AST containing the cursor.
        /// </summary>
        public CommandAst ContainingCommandAst { get; private set; }

        public bool HasCommandAtPosition(IScriptPosition position)
        {
            return ContainingCommandAst is null
                || ContainingCommandAst.CommandElements[0] == ContainingAst
                    && position.Offset == ContainingAst.Extent.EndOffset;
        }

        private static CommandAst GetFirstParentCommandAst(Ast ast)
        {
            do
            {
                if (ast is CommandAst commandAst)
                {
                    return commandAst;
                }

                ast = ast.Parent;
            } while (ast != null);

            return null;
        }

        private static CompletionPositionContext GetEffectiveScriptPosition(
            IScriptPosition cursorPosition,
            IReadOnlyList<Token> tokens)
        {
            // Go backward through the tokens to determine if we're positioned where a new command should be
            Token lastToken = null;
            int lastTokenIndex = -1;
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                Token currToken = tokens[i];

                if (currToken.Extent.EndScriptPosition.LineNumber < cursorPosition.LineNumber
                    || (currToken.Extent.EndScriptPosition.LineNumber == cursorPosition.LineNumber
                        && currToken.Extent.EndScriptPosition.ColumnNumber <= cursorPosition.ColumnNumber))
                {
                    if (lastToken == null)
                    {
                        lastTokenIndex = i;
                        lastToken = currToken;
                        break;
                    }
                }
            }

            if (lastToken.Kind != TokenKind.NewLine)
            {
                return new CompletionPositionContext(lastToken, lastToken, lastToken, lastTokenIndex);
            }

            // Go through and find the first token before us that isn't a newline.
            // When the cursor is at the end of an open scriptblock
            // it falls beyond that scriptblock's extent,
            // meaning we must backtrack to find the real context for a completion
            Token lastNonNewlineToken = null;
            Token firstEndNewlineToken = lastToken;
            for (int i = lastTokenIndex; i >= 0; i--)
            {
                Token currToken = tokens[i];
                if (currToken.Kind != TokenKind.NewLine)
                {
                    lastNonNewlineToken = currToken;
                    break;
                }

                // This becomes the last token we saw moving backward
                // So when we see a non-newline token, this is the one just after that
                firstEndNewlineToken = currToken;
            }

            return new CompletionPositionContext(lastToken, lastNonNewlineToken, firstEndNewlineToken, lastTokenIndex);
        }

        private readonly struct CompletionPositionContext
        {
            public CompletionPositionContext(
                Token lastToken,
                Token lastNonNewlineToken,
                Token effectivePositionToken,
                int lastTokenIndex)
            {
                LastToken = lastToken;
                LastNonNewlineToken = lastNonNewlineToken;
                EffectivePositionToken = effectivePositionToken;
                LastTokenIndex = lastTokenIndex;
            }

            public readonly Token LastToken;

            public readonly Token LastNonNewlineToken;

            public readonly Token EffectivePositionToken;

            public readonly int LastTokenIndex;
        }
    }
}

// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using Azure.Bicep.Types.Concrete;
using PSArm.Commands;
using PSArm.Commands.Template;
using PSArm.Internal;
using PSArm.Schema;
using PSArm.Schema.Keyword;

namespace PSArm.Completion
{
    /// <summary>
    /// Low-level completion provider for the ARM DSL,
    /// designed to work with other completions, possibly overriding them.
    /// </summary>
    public static class DslCompleter
    {
        /// <summary>
        /// Add ARM DSL completions to the front of the completion result collection.
        /// May clobber other results if DSL completions are determined to be of low relevance.
        /// </summary>
        /// <param name="completion">The existing command completion object.</param>
        /// <param name="ast">The AST of the whole input as parsed.</param>
        /// <param name="tokens">The tokens of the whole input as parsed.</param>
        /// <param name="cursorPosition">The position of the cursor within the input.</param>
        /// <param name="options">A completion options hashtable. This is currently ignored.</param>
        public static void PrependDslCompletions(
            CommandCompletion completion,
            Ast ast,
            IReadOnlyList<Token> tokens,
            IScriptPosition cursorPosition,
            Hashtable options)
        {
            Collection<CompletionResult> completionResults = GetCompletions(ast, tokens, cursorPosition, options, out bool clobberCompletions);

            if (completionResults == null)
            {
                return;
            }

            if (completion.ReplacementIndex < 0)
            {
                completion.ReplacementIndex = cursorPosition.Offset;
            }

            if (completion.ReplacementLength < 0)
            {
                completion.ReplacementLength = 0;
            }

            if (clobberCompletions
                || completion.CompletionMatches == null
                || completion.CompletionMatches.Count == 0)
            {
                completion.CompletionMatches = completionResults;
            }
            else
            {
                foreach (CompletionResult existingCompletion in completion.CompletionMatches)
                {
                    completionResults.Add(existingCompletion);
                }
                completion.CompletionMatches = completionResults;
            }
        }

        private static Collection<CompletionResult> GetCompletions(
            Ast ast,
            IReadOnlyList<Token> tokens,
            IScriptPosition cursorPosition,
            Hashtable options,
            out bool clobberCompletions)
        {
            KeywordResult? result = GetCurrentKeyword(ast, tokens, cursorPosition);

            Token lastToken = result?.Frame.ParentContext?.LastToken;

            if (lastToken is null)
            {
                clobberCompletions = false;
                return null;
            }

            KeywordResult keyword = result.Value;

            switch (lastToken.Kind)
            {
                case TokenKind.NewLine:
                case TokenKind.Semi:
                case TokenKind.Pipe:
                case TokenKind.LParen:
                case TokenKind.LCurly:
                case TokenKind.AtParen:
                case TokenKind.DollarParen:
                    clobberCompletions = false;
                    return CompleteKeywords(keyword);

                case TokenKind.Identifier:
                case TokenKind.Command:
                    if (keyword.Frame.ParentContext.HasCommandAtPosition(cursorPosition))
                    {
                        clobberCompletions = false;
                        return CompleteKeywords(keyword);
                    }

                    clobberCompletions = true;
                    return CompleteParameters(keyword, cursorPosition);

                case TokenKind.Generic:
                    if (lastToken.Extent.EndOffset == cursorPosition.Offset)
                    {
                        clobberCompletions = true;
                        return CompleteParameters(keyword, cursorPosition);
                    }
                    break;

                case TokenKind.Parameter:
                    clobberCompletions = true;
                    return CompleteParameters(keyword, cursorPosition);
            }

            clobberCompletions = false;
            return null;
        }

        private static KeywordResult? GetCurrentKeyword(Ast ast, IReadOnlyList<Token> tokens, IScriptPosition cursorPosition)
        {
            KeywordContext context = KeywordContext.BuildFromInput(ast, tokens, cursorPosition);

            if (context is null)
            {
                return null;
            }

            DslKeywordSchema currentSchema = PSArmSchemaInformation.PSArmSchema;
            KeywordContextFrame currentFrame = null;
            for (int i = 0; i < context.KeywordStack.Count; i++)
            {
                KeywordContextFrame frame = context.KeywordStack[i];

                string commandName = frame.CommandAst.GetCommandName();

                if (commandName is null)
                {
                    continue;
                }

                IReadOnlyDictionary<string, DslKeywordSchema> subschemas = currentSchema.GetInnerKeywords(frame);

                if (subschemas is null
                    || !subschemas.TryGetValue(commandName, out DslKeywordSchema subschema))
                {
                    continue;
                }

                currentSchema = subschema;
                currentFrame = frame;
            }

            return new KeywordResult(currentSchema, currentFrame);
        }

        private static Collection<CompletionResult> CompleteParameters(
            KeywordResult keyword,
            IScriptPosition cursorPosition)
        {
            // If we're still on the last token, we need to look further back
            Token lastToken = keyword.Frame.ParentContext.LastToken;
            if (cursorPosition.Offset == lastToken.Extent.EndOffset)
            {
                lastToken = keyword.Frame.ParentContext.Tokens[keyword.Frame.ParentContext.LastTokenIndex - 1];
            }

            return lastToken.Kind == TokenKind.Parameter
                ? CompleteParameterValues(keyword, lastToken)
                : CompleteParameterNames(keyword);
        }

        private static Collection<CompletionResult> CompleteParameterValues(
            KeywordResult keyword,
            Token precedingToken)
        {
            string parameterName = precedingToken.Text.Substring(1);

            IEnumerable<string> values = keyword.Schema.GetParameterValues(keyword.Frame, parameterName);

            if (values is null)
            {
                return null;
            }

            var completions = new Collection<CompletionResult>();
            foreach (string value in values)
            {
                completions.Add(
                    new CompletionResult(
                        value,
                        value,
                        CompletionResultType.ParameterValue,
                        value));
            }

            return completions;
        }

        private static Collection<CompletionResult> CompleteParameterNames(
            KeywordResult keyword)
        {
            IEnumerable<string> parameterNames = keyword.Schema.GetParameterNames(keyword.Frame);

            if (parameterNames is null)
            {
                return null;
            }

            Token lastToken = keyword.Frame.ParentContext.LastToken;
            string prefix = lastToken.Kind == TokenKind.Parameter
                ? lastToken.Text.Substring(1)
                : null;

            var completions = new Collection<CompletionResult>();

            foreach (string parameterName in parameterNames)
            {
                if (prefix != null
                    && !parameterName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string parameterType = keyword.Schema.GetParameterType(keyword.Frame, parameterName);

                string completionText = $"-{parameterName}";
                string completionToolTip = $"[{parameterType}] {parameterName}";
                completions.Add(
                    new CompletionResult(
                        completionText,
                        parameterName,
                        CompletionResultType.ParameterName,
                        completionToolTip));
            }

            return completions;
        }

        private static Collection<CompletionResult> CompleteCmdletKeywords(
            KeywordContext context,
            IReadOnlyDictionary<string, CmdletInfo> keywords)
        {
                string prefix = context.LastToken.Kind == TokenKind.Identifier
                    ? context.LastToken.Text
                    : null;

                var completions = new Collection<CompletionResult>();
                foreach (KeyValuePair<string, CmdletInfo> keyword in keywords)
                {
                    if (prefix == null || keyword.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        completions.Add(
                            new CompletionResult(
                                keyword.Key,
                                keyword.Key,
                                CompletionResultType.Command,
                                keyword.Value.Definition));
                    }
                }
                return completions;
        }

        private static Collection<CompletionResult> CompleteKeywords(KeywordResult keyword)
        {
            Token lastToken = keyword.Frame.ParentContext.LastToken;

            string keywordPrefix = lastToken.Kind == TokenKind.Identifier
                ? lastToken.Text
                : null;

            var completions = new Collection<CompletionResult>();
            foreach (KeyValuePair<string, DslKeywordSchema> innerKeyword in keyword.Schema.GetInnerKeywords(keyword.Frame))
            {
                string keywordName = innerKeyword.Key;
                if (keywordPrefix != null && !keywordName.StartsWith(keywordPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                completions.Add(new CompletionResult(keywordName, keywordName, CompletionResultType.Command, keywordName));
            }

            return completions;
        }

        private readonly struct KeywordResult
        {
            public KeywordResult(
                DslKeywordSchema schema,
                KeywordContextFrame frame)
            {
                Schema = schema;
                Frame = frame;
            }

            public DslKeywordSchema Schema { get; }

            public KeywordContextFrame Frame { get; }
        }
    }
}
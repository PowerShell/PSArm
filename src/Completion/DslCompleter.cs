
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
                    return CompleteKeywords(context);

                case TokenKind.Identifier:
                case TokenKind.Command:
                    if (keyword.ContainingCommandAst == null
                        || (context.ContainingCommandAst.CommandElements[0] == context.ContainingAst
                            && cursorPosition.Offset == context.ContainingAst.Extent.EndOffset))
                    {
                        clobberCompletions = false;
                        return CompleteKeywords(context);
                    }

                    clobberCompletions = true;
                    return CompleteParameters(context);

                case TokenKind.Generic:
                    if (context.LastToken.Extent.EndOffset == cursorPosition.Offset)
                    {
                        clobberCompletions = true;
                        return CompleteParameters(context);
                    }
                    break;

                case TokenKind.Parameter:
                    clobberCompletions = true;
                    return CompleteParameters(context);
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

        private static Collection<CompletionResult> CompleteParameters(KeywordResult keyword)
        {
            string commandName = context?.ContainingCommandAst?.GetCommandName();

            if (commandName == null)
            {
                return null;
            }

            if (context.ResourceNamespace == null || context.KeywordStack.Count < 3)
            {
                // Fall back to the parameter completer
                return null;
            }

            if (!ResourceIndex.SharedInstance.TryGetResourceSchema(context.ResourceNamespace, context.ResourceTypeName, context.ResourceApiVersion, out ResourceSchema resourceSchema))
            {
                return null;
            }

            IReadOnlyDictionary<string, ArmDslKeywordSchema> immediateSchema = GetCurrentKeywordSchema(context, resourceSchema, forParameter: true);

            if (immediateSchema == null
                || !immediateSchema.TryGetValue(commandName, out ArmDslKeywordSchema keywordForContext))
            {
                return null;
            }

            // If we're still on the last token, we need to look further back
            Token beforeToken = context.LastToken;
            if (context.Position.Offset == context.LastToken.Extent.EndOffset)
            {
                beforeToken = context.Tokens[context.LastTokenIndex - 1];
            }

            return beforeToken.Kind == TokenKind.Parameter
                ? CompleteParameterValues(context, keywordForContext.PSKeyword.Parameters, beforeToken)
                : CompleteParameterNames(context, keywordForContext.PSKeyword.Parameters, keywordForContext.Body != null);
        }

        private static Collection<CompletionResult> CompleteParameterValues(
            KeywordContext context,
            IReadOnlyDictionary<string, PSDslParameterInfo> parameters,
            Token precedingToken)
        {
            string parameterName = precedingToken.Text.Substring(1);

            foreach (KeyValuePair<string, PSDslParameterInfo> parameter in parameters)
            {
                if (string.Equals(parameter.Key, parameterName, StringComparison.OrdinalIgnoreCase)
                    && parameter.Value.Parameter.Enum != null)
                {
                    var completions = new Collection<CompletionResult>();
                    foreach (object enumOption in parameter.Value.Parameter.Enum)
                    {
                        string str = enumOption.ToString();
                        completions.Add(
                            new CompletionResult(
                                str,
                                str,
                                CompletionResultType.ParameterValue,
                                str));
                    }
                    return completions;
                }
            }

            return null;
        }

        private static Collection<CompletionResult> CompleteParameterNames(
            KeywordContext context,
            IReadOnlyDictionary<string, PSDslParameterInfo> parameters,
            bool hasBody)
        {
            string prefix = context.LastToken.Kind == TokenKind.Parameter
                ? context.LastToken.Text.Substring(1)
                : null;

            var completions = new Collection<CompletionResult>();

            foreach (KeyValuePair<string, PSDslParameterInfo> parameter in parameters)
            {
                if (prefix != null
                    && !parameter.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string completionText = $"-{parameter.Key}";
                string completionToolTip = $"[{parameter.Value.Parameter.Type}] {parameter.Key}";
                completions.Add(
                    new CompletionResult(
                        completionText,
                        parameter.Key,
                        CompletionResultType.ParameterName,
                        completionToolTip));
            }

            if (hasBody && (prefix == null || "Body".StartsWith(prefix)))
            {
                completions.Add(
                    new CompletionResult(
                        "-Body",
                        "Body",
                        CompletionResultType.ParameterName,
                        "[scriptblock] Body"));
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

        private static Collection<CompletionResult> CompleteKeywords(KeywordContext context)
        {
            // Not in an Arm block
            if (context == null)
            {
                return null;
            }

            // Top level keyword completions
            if (context.KeywordStack.Count == 1)
            {
                return CompleteCmdletKeywords(context, s_topLevelKeywords);
            }

            // Resource completions
            if (context.KeywordStack.Count == 2)
            {
                // Offer top level intra-resource keywords
                return CompleteCmdletKeywords(context, s_resourceKeywords);
            }

            // Now within a resource DSL

            if (!DslLoader.Instance.TryLoadDsl(context.ResourceNamespace, context.ResourceApiVersion, out ArmProviderDslInfo provider))
            {
                return null;
            }

            if (!provider.ProviderSchema.Resources.TryGetValue(context.ResourceTypeName, out ArmDslResourceSchema resourceSchema))
            {
                return null;
            }

            // Top level resource keywords
            if (context.KeywordStack.Count == 3)
            {
                return CompleteKeywordsFromList(context, resourceSchema.Keywords.Values);
            }

            // Deeper keywords
            TypeBase immediateSchema = GetCurrentKeywordSchema(context, resourceSchema.PSKeywordSchema);
            return CompleteKeywordsFromList(context, immediateSchema.Values);
        }

        private static Collection<CompletionResult> CompleteKeywordsFromList(KeywordContext context, IEnumerable<ArmDslKeywordSchema> keywords)
        {
            string keywordPrefix = context.LastToken.Kind == TokenKind.Identifier
                ? context.LastToken.Text
                : null;

            var completions = new Collection<CompletionResult>();
            foreach (ArmDslKeywordSchema keyword in keywords)
            {
                string keywordName = keyword.PSKeyword.Name;
                if (keywordPrefix != null && !keywordName.StartsWith(keywordPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                completions.Add(new CompletionResult(keywordName, keywordName, CompletionResultType.Command, keywordName));
            }

            return completions;
        }

        private static TypeBase GetCurrentKeywordSchema(
            KeywordContext context,
            DslKeywordSchema schema,
            bool forParameter = false)
        {
            string immediateKeyword = null;
            if (forParameter)
            {
                immediateKeyword = context.ContainingCommandAst.GetCommandName();
                if (immediateKeyword == null)
                {
                    return null;
                }
            }

            TypeBase currSchema = resourceSchema.BicepType;
            for (int i = 3; i < context.KeywordStack.Count; i++)
            {
                switch (currSchema)
                {
                }

                if (currSchema == null)
                {
                    return null;
                }

                string keyword = context.KeywordStack[i];

                if (forParameter
                    && string.Equals(keyword, immediateKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (!currSchema.TryGetValue(keyword, out ArmDslKeywordSchema schemaItem))
                {
                    return null;
                }

                if (schemaItem.Body == null)
                {
                    return null;
                }

                currSchema = schemaItem.PSKeywordSchema;
            }

            return currSchema;
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
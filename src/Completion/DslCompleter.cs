using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using PSArm.Commands;
using PSArm.Schema;

namespace PSArm.Completion
{
    public static class DslCompleter
    {
        private static Dictionary<string, bool> s_ignoredCommands = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "ForEach-Object", true },
            { "%", true },
        };

        private static CmdletInfo s_armInfo = new CmdletInfo("New-ArmTemplate", typeof(NewArmTemplateCommand));

        private static CmdletInfo s_resourceInfo = new CmdletInfo("New-ArmResource", typeof(NewArmResourceCommand));

        private static CmdletInfo s_outputInfo = new CmdletInfo("New-ArmOutput", typeof(NewArmOutputCommand));

        private static CmdletInfo s_propertiesInfo = new CmdletInfo("New-ArmProperties", typeof(NewArmPropertiesCommand));

        private static CmdletInfo s_dependsOnInfo = new CmdletInfo("New-ArmDependsOn", typeof(NewArmDependsOnCommand));

        private static CmdletInfo s_skuInfo = new CmdletInfo("New-ArmSku", typeof(NewArmSkuCommand));

        private static readonly char[] s_typeSplitChars = new [] { '/' };

        private static readonly IReadOnlyDictionary<string, CmdletInfo> s_topLevelKeywords = new Dictionary<string, CmdletInfo>
        {
            { "Resource", s_resourceInfo },
            { "Output", s_outputInfo },
        };

        private static readonly IReadOnlyDictionary<string, CmdletInfo> s_resourceKeywords = new Dictionary<string, CmdletInfo>
        {
            { "Resource", s_resourceInfo },
            { "Properties", s_propertiesInfo },
            { "DependsOn", s_dependsOnInfo },
            { "Sku", s_skuInfo },
        };

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
                    }
                }
            }

            if (lastToken == null)
            {
                clobberCompletions = false;
                return null;
            }

            KeywordContext context = GetKeywordContext(ast, tokens, lastTokenIndex, cursorPosition);

            if (context == null)
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
                    if (context.ContainingCommandAst == null
                        || (context.ContainingCommandAst.CommandElements[0] == context.ContainingAst
                            && cursorPosition.Offset == context.ContainingAst.Extent.EndOffset))
                    {
                        clobberCompletions = false;
                        return CompleteKeywords(context);
                    }

                    clobberCompletions = true;
                    return CompleteParameters(context);

                case TokenKind.Generic:
                    if (lastToken.Extent.EndOffset == cursorPosition.Offset)
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

        private static Collection<CompletionResult> CompleteParameters(KeywordContext context)
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

            if (!DslLoader.Instance.TryLoadDsl(context.ResourceNamespace, out ArmDslInfo dslInfo)
                || !dslInfo.Schema.Subschemas.TryGetValue(context.ResourceTypeName, out Dictionary<string, DslSchemaItem> schema))
            {
                return null;
            }

            IReadOnlyDictionary<string, DslSchemaItem> immediateSchema = GetCurrentKeywordSchema(context, schema, forParameter: true);

            if (immediateSchema == null
                || !immediateSchema.TryGetValue(commandName, out DslSchemaItem keywordSchemaItem))
            {
                return null;
            }

            // If we're still on the last token, we need to look further back
            Token beforeToken = context.LastToken;
            string prefix = null;
            if (context.Position.Offset == context.LastToken.Extent.EndOffset)
            {
                beforeToken = context.Tokens[context.LastTokenIndex - 1];
                prefix = context.LastToken.Text;
            }

            return beforeToken.Kind == TokenKind.Parameter
                ? CompleteParameterValues(context, keywordSchemaItem.Parameters, beforeToken)
                : CompleteParameterNames(context, keywordSchemaItem.Parameters);
        }

        private static Collection<CompletionResult> CompleteParameterValues(
            KeywordContext context,
            IReadOnlyList<DslParameter> parameters,
            Token precedingToken)
        {
            string parameterName = precedingToken.Text.Substring(1);
            foreach (DslParameter parameter in parameters)
            {
                if (string.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase)
                    && parameter.Enum != null)
                {
                    var completions = new Collection<CompletionResult>();
                    foreach (object enumOption in parameter.Enum)
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
            IReadOnlyList<DslParameter> parameters)
        {
            string prefix = context.LastToken.Kind == TokenKind.Parameter
                ? context.LastToken.Text.Substring(1)
                : null;
            var completions = new Collection<CompletionResult>();
            foreach (DslParameter parameter in parameters)
            {
                if (prefix != null
                    && !parameter.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string completionText = $"-{parameter.Name}";
                string completionToolTip = $"[{parameter.Type}] {parameter.Name}";
                completions.Add(
                    new CompletionResult(
                        completionText,
                        parameter.Name,
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

            if (!DslLoader.Instance.TryLoadDsl(context.ResourceNamespace, out ArmDslInfo dslInfo))
            {
                return null;
            }

            if (!dslInfo.Schema.Subschemas.TryGetValue(context.ResourceTypeName, out Dictionary<string, DslSchemaItem> schema))
            {
                return null;
            }

            // Top level resource keywords
            if (context.KeywordStack.Count == 3)
            {
                return CompleteKeywordsFromList(context, schema.Keys);
            }

            // Deeper keywords
            IReadOnlyDictionary<string, DslSchemaItem> immediateSchema = GetCurrentKeywordSchema(context, schema);
            return CompleteKeywordsFromList(context, immediateSchema.Keys);
        }

        private static Collection<CompletionResult> CompleteKeywordsFromList(KeywordContext context, IEnumerable<string> keywords)
        {
            string keywordPrefix = context.LastToken.Kind == TokenKind.Identifier
                ? context.LastToken.Text
                : null;

            var completions = new Collection<CompletionResult>();
            foreach (string keyword in keywords)
            {
                if (keywordPrefix != null && !keyword.StartsWith(keywordPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                completions.Add(new CompletionResult(keyword, keyword, CompletionResultType.Command, keyword));
            }

            return completions;
        }

        private static IReadOnlyDictionary<string, DslSchemaItem> GetCurrentKeywordSchema(
            KeywordContext context,
            IReadOnlyDictionary<string, DslSchemaItem> schema,
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

            IReadOnlyDictionary<string, DslSchemaItem> currSchema = schema;
            for (int i = 3; i < context.KeywordStack.Count; i++)
            {
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

                if (!currSchema.TryGetValue(keyword, out DslSchemaItem schemaItem))
                {
                    return null;
                }

                switch (schemaItem)
                {
                    case DslBlockSchema block:
                        currSchema = block.Body;
                        break;

                    case DslArraySchema array:
                        currSchema = array.Body;
                        break;

                    default:
                        return null;
                }
            }

            return currSchema;
        }

        private static KeywordContext GetKeywordContext(
            Ast ast,
            IReadOnlyList<Token> tokens,
            int lastTokenIndex,
            IScriptPosition cursorPosition)
        {
            Token lastToken = tokens[lastTokenIndex];
            Token lastNonNewlineToken = null;
            for (int i = lastTokenIndex; i >= 0; i--)
            {
                Token currToken = tokens[i];
                if (currToken.Kind != TokenKind.NewLine)
                {
                    lastNonNewlineToken = currToken;
                    break;
                }
            }

            // When the cursor is at the end of an open scriptblock
            // it falls beyond that scriptblock's extent,
            // meaning we must backtrack to find the real context for a completion
            IScriptPosition effectiveCompletionPosition;
            switch (lastNonNewlineToken.Kind)
            {
                case TokenKind.Identifier:
                case TokenKind.Generic:
                case TokenKind.Command:
                    effectiveCompletionPosition = cursorPosition;
                    break;

                default:
                    effectiveCompletionPosition = lastNonNewlineToken.Extent.EndScriptPosition;
                    break;
            }

            // Now find the AST we're in
            var visitor = new FindAstFromPositionVisitor(effectiveCompletionPosition);
            ast.Visit(visitor);
            Ast containingAst = visitor.GetAst();

            if (containingAst == null)
            {
                return null;
            }

            CommandAst containingCommandAst = GetFirstParentCommandAst(containingAst);

            var context = new KeywordContext
            {
                ContainingAst = containingAst,
                ContainingCommandAst = containingCommandAst,
                FullAst = ast,
                LastTokenIndex = lastTokenIndex,
                LastToken = lastToken,
                LastNonNewlineToken = lastNonNewlineToken,
                Tokens = tokens,
                Position = cursorPosition
            };

            Ast currAst = containingAst;
            bool foundArmKeyword = false;
            do
            {
                if (currAst is ScriptBlockExpressionAst sbAst
                    && sbAst.Parent is CommandAst commandAst)
                {
                    string commandName = commandAst.GetCommandName();

                    if (!s_ignoredCommands.ContainsKey(commandName))
                    {
                        context.KeywordStack.Add(commandName);

                        if (string.Equals(commandName, "Resource", StringComparison.OrdinalIgnoreCase))
                        {
                            SetContextResourceInfo(context, commandAst);
                        }
                        else if (string.Equals(commandName, "Arm", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(commandName, "New-ArmTemplate", StringComparison.OrdinalIgnoreCase))
                        {
                            foundArmKeyword = true;
                            break;
                        }
                    }
                }

                currAst = currAst.Parent;
            } while (currAst != null);

            if (!foundArmKeyword)
            {
                return null;
            }

            if (context.KeywordStack.Count > 1)
            {
                context.KeywordStack.Reverse();
            }

            return context;
        }

        private static void SetContextResourceInfo(KeywordContext context, CommandAst commandAst)
        {
            int expect = 0;
            for (int i = 0; i < commandAst.CommandElements.Count; i++)
            {
                CommandElementAst element = commandAst.CommandElements[i];

                if (element is CommandParameterAst parameterAst)
                {
                    expect = 0;
                    if (string.Equals(parameterAst.ParameterName, "Type", StringComparison.OrdinalIgnoreCase))
                    {
                        expect = 1;
                    }
                    else if (string.Equals(parameterAst.ParameterName, "ApiVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        expect = 2;
                    }

                    continue;
                }

                switch (expect)
                {
                    case 1:
                        if (element is StringConstantExpressionAst typeStrExpr)
                        {
                            string[] typeParts = typeStrExpr.Value.Split(s_typeSplitChars, count: 2);
                            context.ResourceNamespace = typeParts[0];
                            context.ResourceTypeName = typeParts[1];
                        }
                        break;

                    case 2:
                        if (element is StringConstantExpressionAst apiVersionStrExpr)
                        {
                            context.ResourceApiVersion = apiVersionStrExpr.Value;
                        }
                        break;
                }

                expect = 0;
            }
        }
    }
}
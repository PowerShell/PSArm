using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSArm
{
    public static class DslCompleter
    {
        private static CmdletInfo s_armInfo = new CmdletInfo("New-ArmTemplate", typeof(NewArmTemplateCommand));

        private static CmdletInfo s_resourceInfo = new CmdletInfo("New-ArmResource", typeof(NewArmResourceCommand));

        private static readonly char[] s_typeSplitChars = new [] { '/' };

        private static readonly IReadOnlyDictionary<string, CmdletInfo> s_topLevelKeywords = new Dictionary<string, CmdletInfo>
        {
            { "Resource", s_resourceInfo },
        };

        public static void PrependDslCompletions(
            CommandCompletion completion,
            Ast ast,
            IReadOnlyList<Token> tokens,
            IScriptPosition cursorPosition,
            Hashtable options)
        {
            Collection<CompletionResult> completionResults = GetCompletions(ast, tokens, cursorPosition, options, out int replacementLength);

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
                completion.ReplacementLength = replacementLength;
            }

            if (completion.CompletionMatches == null || completion.CompletionMatches.Count > 0)
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
            out int replacementLength)
        {
            // Go backward through the tokens to determine if we're positioned where a new command should be
            Token lastToken = null;
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                lastToken = tokens[i];

                if (lastToken.Kind == TokenKind.NewLine)
                {
                    continue;
                }

                if (lastToken.Extent.EndScriptPosition.LineNumber < cursorPosition.LineNumber
                    || (lastToken.Extent.EndScriptPosition.LineNumber == cursorPosition.LineNumber
                        && lastToken.Extent.EndScriptPosition.ColumnNumber <= cursorPosition.ColumnNumber))
                {
                    break;
                }
            }

            if (lastToken == null)
            {
                replacementLength = -1;
                return null;
            }

            KeywordContext context;
            switch (lastToken.Kind)
            {
                case TokenKind.NewLine:
                case TokenKind.Semi:
                case TokenKind.Pipe:
                case TokenKind.LParen:
                case TokenKind.LCurly:
                case TokenKind.AtParen:
                case TokenKind.DollarParen:
                    // We need this trick to put us back into an incomplete scriptblock
                    // if we were asked for a completion at the end of an incomplete one
                    context = GetKeywordContext(
                        ast,
                        lastToken,
                        tokens,
                        cursorPosition,
                        lastToken.Extent.EndScriptPosition);
                    replacementLength = 0;
                    return CompleteKeywords(context);

                case TokenKind.Identifier:
                case TokenKind.Command:
                    context = GetKeywordContext(
                        ast,
                        lastToken,
                        tokens,
                        cursorPosition);
                    replacementLength = 0;
                    return CompleteKeywords(context);

                case TokenKind.Generic:
                    if (lastToken.Text == "-"
                        && lastToken.Extent.EndOffset == cursorPosition.Offset
                        && ast.Parent is CommandElementAst)
                    {
                        context = GetKeywordContext(ast, lastToken, tokens, cursorPosition);
                        replacementLength = 0;
                        return CompleteParameters(context);
                    }
                    break;

                case TokenKind.Parameter:
                    context = GetKeywordContext(ast, lastToken, tokens, cursorPosition);
                    replacementLength = lastToken.Text.Length;
                    return CompleteParameters(context);
            }

            replacementLength = -1;
            return null;
        }

        private static Collection<CompletionResult> CompleteParameters(KeywordContext context)
        {
            if (context == null)
            {
                return null;
            }

            if (context.ResourceNamespace == null)
            {
                // Fall back to the parameter completer
                return null;
            }

            return null;
        }

        private static Collection<CompletionResult> CompleteKeywords(KeywordContext context)
        {
            if (context == null)
            {
                return null;
            }

            // Top level keyword completions
            if (context.ResourceNamespace == null)
            {
                string prefix = context.LastToken.Kind == TokenKind.Identifier
                    ? context.LastToken.Text
                    : null;

                var completions = new Collection<CompletionResult>();
                foreach (KeyValuePair<string, CmdletInfo> topLevelKeyword in s_topLevelKeywords)
                {
                    if (prefix == null || topLevelKeyword.Key.StartsWith(prefix))
                    {
                        completions.Add(
                            new CompletionResult(
                                topLevelKeyword.Key,
                                topLevelKeyword.Key,
                                CompletionResultType.Command,
                                topLevelKeyword.Value.Definition));
                    }
                }
                return completions;
            }


            if (!DslLoader.Instance.TryLoadDsl(context.ResourceNamespace, out ArmDslInfo dslInfo))
            {
                return null;
            }

            if (!dslInfo.Schema.Subschemas.TryGetValue(context.ResourceTypeName, out Dictionary<string, DslSchemaItem> schema))
            {
                return null;
            }

            if (context.KeywordStack.Count == 0)
            {
                return CompleteKeywordsFromList(context, schema.Keys);
            }

            if (context.KeywordStack.Count < 2)
            {
                return null;
            }

            var immediateSchema = schema[context.KeywordStack[1]] as DslBlockSchema;
            for (int i = 2; i < context.KeywordStack.Count; i++)
            {
                if (immediateSchema == null)
                {
                    return null;
                }

                string currKeyword = context.KeywordStack[i];
                if (!immediateSchema.Body.TryGetValue(currKeyword, out DslSchemaItem subschema)
                    || !(subschema is DslBlockSchema subBlock))
                {
                    return null;
                }

                immediateSchema = subBlock;
            }

            return CompleteKeywordsFromList(context, immediateSchema.Body.Keys);
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

        private static DslSchemaItem GetKeywordWithStack(IEnumerable<string> keywordStack, DslSchemaItem schemaItem)
        {
            DslSchemaItem currSchema = schemaItem;
            foreach (string keyword in keywordStack)
            {
                switch (currSchema)
                {
                    case DslBlockSchema block:
                        if (!block.Body.TryGetValue(keyword, out currSchema))
                        {
                            return null;
                        }
                        continue;

                    case DslArraySchema array:
                        if (array.Body == null
                            || !array.Body.TryGetValue(keyword, out currSchema))
                        {
                            return null;
                        }
                        continue;

                    default:
                        return null;
                }
            }
            return currSchema;
        }

        private static KeywordContext GetKeywordContext(
            Ast ast,
            Token lastToken,
            IReadOnlyList<Token> tokens,
            IScriptPosition cursorPosition,
            IScriptPosition completionPosition = null)
        {
            if (completionPosition == null)
            {
                completionPosition = cursorPosition;
            }

            // Now find the AST we're in
            var visitor = new FindAstFromPositionVisitor(completionPosition);
            ast.Visit(visitor);
            Ast containingAst = visitor.GetAst();

            if (containingAst == null)
            {
                return null;
            }

            var context = new KeywordContext
            {
                ContainingAst = containingAst,
                FullAst = ast,
                LastToken = lastToken,
                Tokens = tokens,
                Position = cursorPosition
            };

            Ast currAst = containingAst;
            bool foundArmKeyword = false;
            do
            {
                if (currAst is CommandAst commandAst)
                {
                    string commandName = commandAst.GetCommandName();

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
                    else
                    {
                        context.KeywordStack.Add(commandName);
                    }
                }

                currAst = currAst.Parent;
            } while (currAst != null);

            if (!foundArmKeyword)
            {
                return null;
            }

            if (context.KeywordStack.Count == 0)
            {
                return context;
            }

            context.KeywordStack.Reverse();

            // If we're completing a keyword, remove it from the stack
            if (lastToken.Kind == TokenKind.Identifier)
            {
                context.KeywordStack.RemoveAt(context.KeywordStack.Count - 1);
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
                            string[] typeParts = typeStrExpr.Value.Split(s_typeSplitChars);
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

    internal class KeywordContext
    {
        public KeywordContext()
        {
            KeywordStack = new List<string>();
        }

        public List<string> KeywordStack { get; }

        public string ResourceNamespace { get; set; }

        public string ResourceTypeName { get; set; }

        public string ResourceApiVersion { get; set; }

        public Ast ContainingAst { get; set; }

        public Ast FullAst { get; set; }

        public IReadOnlyList<Token> Tokens { get; set; }

        public Token LastToken { get; set; }

        public IScriptPosition Position { get; set; }
    }

    internal class FindAstFromPositionVisitor : AstVisitor2
    {
        private readonly IScriptPosition _position;

        private Ast _astAtPosition;

        public FindAstFromPositionVisitor(IScriptPosition position)
        {
            _position = position;
        }

        public Ast GetAst()
        {
            return _astAtPosition;
        }

        public override AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) => VisitAst(arrayExpressionAst);

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) => VisitAst(arrayLiteralAst);

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) => VisitAst(assignmentStatementAst);

        public override AstVisitAction VisitAttribute(AttributeAst attributeAst) => VisitAst(attributeAst);

        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) => VisitAst(attributedExpressionAst);

        public override AstVisitAction VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) => AstVisitAction.SkipChildren;

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) => VisitAst(binaryExpressionAst);

        public override AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst) => VisitAst(blockStatementAst);

        public override AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst) => VisitAst(breakStatementAst);

        public override AstVisitAction VisitCatchClause(CatchClauseAst catchClauseAst) => VisitAst(catchClauseAst);

        public override AstVisitAction VisitCommand(CommandAst commandAst) => VisitAst(commandAst);

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst) => VisitAst(commandExpressionAst);

        public override AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst) => VisitAst(commandParameterAst);

        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst) => VisitAst(configurationDefinitionAst);

        public override AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst) => VisitAst(constantExpressionAst);

        public override AstVisitAction VisitContinueStatement(ContinueStatementAst continueStatementAst) => VisitAst(continueStatementAst);

        public override AstVisitAction VisitConvertExpression(ConvertExpressionAst convertExpressionAst) => VisitAst(convertExpressionAst);

        public override AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst) => VisitAst(dataStatementAst);

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) => VisitAst(doUntilStatementAst);

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) => VisitAst(doWhileStatementAst);

        public override AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst) => VisitAst(dynamicKeywordStatementAst);

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst) => VisitAst(errorExpressionAst);

        public override AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst) => VisitAst(errorStatementAst);

        public override AstVisitAction VisitExitStatement(ExitStatementAst exitStatementAst) => VisitAst(exitStatementAst);

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) => VisitAst(expandableStringExpressionAst);

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst) => VisitAst(redirectionAst);

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst) => VisitAst(forEachStatementAst);

        public override AstVisitAction VisitForStatement(ForStatementAst forStatementAst) => VisitAst(forStatementAst);

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) => VisitAst(functionDefinitionAst);

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst) => VisitAst(functionMemberAst);

        public override AstVisitAction VisitHashtable(HashtableAst hashtableAst) => VisitAst(hashtableAst);

        public override AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst) => VisitAst(ifStmtAst);
    
        public override AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst) => VisitAst(indexExpressionAst);

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst) => VisitAst(methodCallAst);

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst) => VisitAst(memberExpressionAst);

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst) => VisitAst(redirectionAst);

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) => VisitAst(namedAttributeArgumentAst);

        public override AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst) => VisitAst(namedBlockAst);

        public override AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst) => VisitAst(paramBlockAst);

        public override AstVisitAction VisitParameter(ParameterAst parameterAst) => VisitAst(parameterAst);

        public override AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst) => VisitAst(parenExpressionAst);

        public override AstVisitAction VisitPipeline(PipelineAst pipelineAst) => VisitAst(pipelineAst);

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst) => VisitAst(propertyMemberAst);

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst) => VisitAst(returnStatementAst);

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst) => VisitAst(scriptBlockAst);

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) => VisitAst(scriptBlockExpressionAst);

        public override AstVisitAction VisitStatementBlock(StatementBlockAst statementBlockAst) => VisitAst(statementBlockAst);

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) => VisitAst(stringConstantExpressionAst);

        public override AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst) => VisitAst(subExpressionAst);

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst) => VisitAst(switchStatementAst);

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst) => VisitAst(throwStatementAst);

        public override AstVisitAction VisitTrap(TrapStatementAst trapStatementAst) => VisitAst(trapStatementAst);

        public override AstVisitAction VisitTryStatement(TryStatementAst tryStatementAst) => VisitAst(tryStatementAst);

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst) => VisitAst(typeConstraintAst);

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) => VisitAst(typeDefinitionAst);

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst) => VisitAst(typeExpressionAst);

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) => VisitAst(unaryExpressionAst);

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst) => VisitAst(usingExpressionAst);

        public override AstVisitAction VisitUsingStatement(UsingStatementAst usingStatementAst) => VisitAst(usingStatementAst);

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst) => VisitAst(variableExpressionAst);

        public override AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst) => VisitAst(whileStatementAst);

        private AstVisitAction VisitAst(Ast ast)
        {
            if (!AstContainsPosition(ast))
            {
                return AstVisitAction.SkipChildren;
            }

            _astAtPosition = ast;
            return AstVisitAction.Continue;
        }

        private bool AstContainsPosition(Ast ast)
        {
            return IsBefore(ast.Extent.StartScriptPosition, _position)
                && IsBefore(_position, ast.Extent.EndScriptPosition);
        }

        private static bool IsBefore(IScriptPosition left, IScriptPosition right)
        {
            return left.LineNumber < right.LineNumber
                || (left.LineNumber == right.LineNumber && left.ColumnNumber <= right.ColumnNumber);
        }
    }

    public class ResourceArgumentCompleter : IArgumentCompleter
    {
        private static string[] s_locations = new []
        {
            "WestUS",
            "WestUS2",
            "CentralUS"
        };

        private static char[] s_typeSeparator = new [] { '/' };

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            if (IsString(parameterName, "Type"))
            {
                var completions = new List<CompletionResult>();
                if (wordToComplete.Contains("/"))
                {
                    string[] completeParts = wordToComplete.Split(s_typeSeparator);
                    if (DslLoader.Instance.TryLoadDsl(completeParts[0], out ArmDslInfo dslInfo))
                    {
                        foreach (string subschema in dslInfo.Schema.Subschemas.Keys)
                        {
                            if (subschema.StartsWith(completeParts[1]))
                            {
                                string fullCompletion = $"{completeParts[0]}/{subschema}";
                                completions.Add(new CompletionResult(fullCompletion, fullCompletion, CompletionResultType.ParameterValue, fullCompletion));
                            }
                        }
                    }

                    return completions;
                }

                foreach (string schemaName in DslLoader.Instance.ListSchemas())
                {
                    if (schemaName.StartsWith(wordToComplete))
                    {
                        string completion = $"{schemaName}/";
                        completions.Add(new CompletionResult(completion, completion, CompletionResultType.ParameterValue, completion));
                    }
                }

                return completions;
            }

            if (IsString(parameterName, "Location"))
            {
                return GetCompletionsFromList(wordToComplete, s_locations);
            }

            return null;
        }

        private static IEnumerable<CompletionResult> GetCompletionsFromList(string prefix, IEnumerable<string> possibleValues)
        {
            foreach (string possibleValue in possibleValues)
            {
                if (!possibleValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return new CompletionResult(possibleValue, possibleValue, CompletionResultType.ParameterValue, possibleValue);
            }
        }

        private static bool IsString(string str, string expected)
        {
            return string.Equals(str, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
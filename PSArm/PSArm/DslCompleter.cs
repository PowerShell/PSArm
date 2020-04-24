using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm
{
    public static class DslCompleter
    {
        private static readonly char[] s_typeSplitChars = new [] { '/' };

        public static CommandCompletion CompleteInput(string input, int cursorIndex, Hashtable options)
        {
            Tuple<Ast, Token[], IScriptPosition> parsedInput = CommandCompletion.MapStringInputToParsedInput(input, cursorIndex);
            return CompleteInput(parsedInput.Item1, parsedInput.Item2, parsedInput.Item3, options);
        }

        public static CommandCompletion CompleteInput(
            Ast ast,
            IReadOnlyList<Token> tokens,
            IScriptPosition cursorPosition,
            Hashtable options)
        {
            // Go backward through the tokens to determine if we're positioned where a new command should be
            Token lastToken = null;
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                lastToken = tokens[i];
                if (lastToken.Extent.EndScriptPosition.LineNumber < cursorPosition.LineNumber
                    || (lastToken.Extent.EndScriptPosition.LineNumber == cursorPosition.LineNumber
                        && lastToken.Extent.EndScriptPosition.ColumnNumber == cursorPosition.ColumnNumber))
                {
                    break;
                }
            }

            if (lastToken == null)
            {
                return null;
            }

            IScriptPosition completionPosition = cursorPosition;
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
                    completionPosition = lastToken.Extent.EndScriptPosition;
                    break;

                case TokenKind.Identifier:
                case TokenKind.Command:
                    break;

                default:
                    return null;
            }

            // Now find the AST we're in
            var visitor = new FindAstFromPositionVisitor(completionPosition);
            ast.Visit(visitor);
            Ast containingAst = visitor.GetAst();

            if (containingAst == null)
            {
                return null;
            }

            KeywordContext keywordContext = GetKeywordContext(containingAst, ast, lastToken, tokens, cursorPosition);

            if (keywordContext == null
                || !DslLoader.Instance.TryLoadDsl(keywordContext.ResourceNamespace, out ArmDslInfo dslInfo))
            {
                return null;
            }

            return GetCompletionsForContext(keywordContext, dslInfo);
        }

        private static CommandCompletion GetCompletionsForContext(KeywordContext context, ArmDslInfo dslInfo)
        {
            if (!dslInfo.Schema.Subschemas.TryGetValue(context.ResourceTypeName, out Dictionary<string, DslSchemaItem> schema))
            {
                return null;
            }

            if (context.KeywordStack.Count == 0)
            {
                return GetCompletionForKeywordCommands(context, schema.Keys);
            }

            return null;
        }

        private static CommandCompletion GetCompletionForKeywordCommands(KeywordContext context, IEnumerable<string> keywords)
        {
            string keywordPrefix = context.LastToken.Kind == TokenKind.Command
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

            return new CommandCompletion(completions, currentMatchIndex: -1, replacementIndex: context.Position.ColumnNumber, replacementLength: 0);
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

        private static KeywordContext GetKeywordContext(Ast ast, Ast fullAst, Token preToken, IReadOnlyList<Token> tokens, IScriptPosition position)
        {
            var context = new KeywordContext
            {
                ContainingAst = ast,
                FullAst = fullAst,
                LastToken = preToken,
                Tokens = tokens,
                Position = position
            };

            Ast currAst = ast;
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

            context.KeywordStack.Reverse();

            // If we're completing a keyword, remove it from the stack
            if (preToken.Kind == TokenKind.Identifier)
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
}
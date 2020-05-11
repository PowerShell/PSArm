using System.Management.Automation.Language;

namespace PSArm.Completion
{
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

// Copyright (c) Microsoft Corporation.

using PSArm.Internal;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace PSArm.Parameterization
{
    internal class ReferenceCollectingPSAstVisitor : AstVisitor2
    {
        private readonly HashSet<string> _variablesToFind;

        private readonly Dictionary<string, List<VariableExpressionAst>> _references;

        public ReferenceCollectingPSAstVisitor(HashSet<string> variablesToFind)
            : this()
        {
            _variablesToFind = variablesToFind;
        }

        public ReferenceCollectingPSAstVisitor()
        {
            _references = new Dictionary<string, List<VariableExpressionAst>>();
        }

        public IReadOnlyDictionary<string, List<VariableExpressionAst>> References => _references;

        public void Reset()
        {
            _references.Clear();
        }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            string variableName = variableExpressionAst.VariablePath.UserPath;

            if (_variablesToFind is null
                || _variablesToFind.Contains(variableName))
            {
                _references.AddToDictionaryList(variableExpressionAst.VariablePath.UserPath, variableExpressionAst);
            }

            return AstVisitAction.Continue;
        }
    }
}

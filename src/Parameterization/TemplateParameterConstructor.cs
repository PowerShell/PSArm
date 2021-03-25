// Copyright (c) Microsoft Corporation.

using PSArm.Templates;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PSArm.Parameterization
{
    internal abstract class TemplateParameterConstructor<TArmParameter, TParameters, TParameter, TParameterName>
        where TArmParameter : ArmElement, IArmReferenceable
    {
        public ArmObject<TArmParameter> ConstructParameters(TParameters parameters)
        {
            IReadOnlyDictionary<TParameter, IReadOnlyList<TParameterName>> referenceTable = CollectReferences(parameters);

            var parameterBlock = new ArmObject<TArmParameter>();
            foreach (TParameter parameter in GetParameterEvaluationOrder(referenceTable))
            {
                TArmParameter armParameter = EvaluateParameter(parameter);
                parameterBlock[armParameter.ReferenceName] = armParameter;
            }
            return parameterBlock;
        }

        protected abstract IReadOnlyDictionary<TParameter, IReadOnlyList<TParameterName>> CollectReferences(TParameters parameters);

        protected abstract TArmParameter EvaluateParameter(TParameter parameter);

        protected abstract TParameterName GetParameterName(TParameter parameter);

        private IEnumerable<TParameter> GetParameterEvaluationOrder(IReadOnlyDictionary<TParameter, IReadOnlyList<TParameterName>> referenceTable)
        {
            // Build a table of parameters by name for faster reference
            var parameterTable = new Dictionary<TParameterName, TParameter>(referenceTable.Count);
            foreach (TParameter parameter in referenceTable.Keys)
            {
                parameterTable[GetParameterName(parameter)] = parameter;
            }

            // Now use our searcher to run the DFS evaluation order algorithm
            return new DfsEvaluationOrderSearcher<TParameterName, TParameter>(parameterTable, referenceTable).GetEvaluationOrder();
        }

        /// <summary>
        /// Implements a topological sorting algorithm described in https://en.wikipedia.org/wiki/Topological_sorting#Depth-first_search.
        /// This uses depth-first search to traverse the parameter graph and give us a safe order of evaluation of parameters,
        /// or otherwise determines that there's a cycle and throws an exception.
        /// </summary>
        /// <typeparam name="TKey">The node label/name, or parameter name.</typeparam>
        /// <typeparam name="TValue">The node value, or parameter.</typeparam>
        private class DfsEvaluationOrderSearcher<TKey, TValue>
        {
            private readonly IReadOnlyDictionary<TValue, IReadOnlyList<TKey>> _referenceTable;

            private readonly IReadOnlyDictionary<TKey, TValue> _nodeTable;

            private readonly HashSet<TValue> _unmarkedNodes;

            private readonly HashSet<TValue> _completedNodes;

            private readonly HashSet<TValue> _currentSearchNodes;

            private readonly Queue<TValue> _evaluationOrder;

            public DfsEvaluationOrderSearcher(
                IReadOnlyDictionary<TKey, TValue> nodeTable,
                IReadOnlyDictionary<TValue, IReadOnlyList<TKey>> referenceTable)
            {
                _nodeTable = nodeTable;
                _referenceTable = referenceTable;
                _unmarkedNodes = new HashSet<TValue>(_nodeTable.Values);
                _completedNodes = new HashSet<TValue>();
                _currentSearchNodes = new HashSet<TValue>();
                _evaluationOrder = new Queue<TValue>();
            }

            public IEnumerable<TValue> GetEvaluationOrder()
            {
                while (HasUnmarkedNodes())
                {
                    TValue currentNode = GetUnmarkedNode();
                    DoDfs(currentNode);
                }

                return _evaluationOrder;
            }

            private void DoDfs(TValue currentNode)
            {
                // We've already done DFS from this node, so go no further
                if (_completedNodes.Contains(currentNode))
                {
                    return;
                }

                // We're encountering this node for the second time, so we've found a cycle
                if (_currentSearchNodes.Contains(currentNode))
                {
                    throw new ArgumentException($"Cyclic parameter dependency detected: the parameter '{currentNode}' depends on itself");
                }

                // Before starting on this node, add 
                _currentSearchNodes.Add(currentNode);

                // Do the actual DFS here
                foreach (TKey nodeName in _referenceTable[currentNode])
                {
                    TValue nextNode = _nodeTable[nodeName];
                    DoDfs(nextNode);
                }

                // Once we've gotten here, we've already visited any child nodes
                MarkNode(currentNode);
            }

            private bool HasUnmarkedNodes()
            {
                return _unmarkedNodes.Count > 0;
            }

            private TValue GetUnmarkedNode()
            {
                return _unmarkedNodes.First();
            }

            private void MarkNode(TValue node)
            {
                _currentSearchNodes.Remove(node);
                _completedNodes.Add(node);
                _unmarkedNodes.Remove(node);
                _evaluationOrder.Enqueue(node);
            }
        }
    }
}

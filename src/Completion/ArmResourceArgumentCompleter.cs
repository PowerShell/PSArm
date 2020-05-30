
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using PSArm.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm.Completion
{
    /// <summary>
    /// Argument completer for cmdlet arguments that take an ARM resource type,
    /// such as 'Microsoft.Network/publicIpAddresses'.
    /// </summary>
    public class ArmResourceArgumentCompleter : IArgumentCompleter
    {
        private static string[] s_locations = new []
        {
            "WestUS",
            "WestUS2",
            "CentralUS"
        };

        private static char[] s_typeSeparator = new [] { '/' };

        /// <summary>
        /// Complete an ARM resource "type" argument.
        /// </summary>
        /// <param name="commandName">The name of the invoked command whose argument this is.</param>
        /// <param name="parameterName">The name of the parameter for the argument, if any.</param>
        /// <param name="wordToComplete">The argument so far.</param>
        /// <param name="commandAst">The entirety of command AST in which this argument is being provided.</param>
        /// <param name="fakeBoundParameters">The attempted parameter binding, for providing something similar to $PSBoundParameters.</param>
        /// <returns></returns>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            if (IsString(parameterName, "Type"))
            {
                var completions = new List<CompletionResult>();
                if (wordToComplete.Contains("/"))
                {
                    string[] completeParts = wordToComplete.Split(s_typeSeparator, count: 2);
                    if (DslLoader.Instance.TryLoadDsl(completeParts[0], out ArmDslInfo dslInfo))
                    {
                        foreach (string subschema in dslInfo.Schema.Subschemas.Keys)
                        {
                            if (subschema.StartsWith(completeParts[1], StringComparison.OrdinalIgnoreCase))
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
                    if (schemaName.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
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
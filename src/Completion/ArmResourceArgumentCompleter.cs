
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using PSArm.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;

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
                string apiVersion = fakeBoundParameters.Contains("ApiVersion")
                    ? (string)fakeBoundParameters["ApiVersion"]
                    : null;

                var completions = new List<CompletionResult>();
                if (wordToComplete.Contains("/"))
                {
                    // We can't hope to load all schemas, if we have no version
                    if (apiVersion == null)
                    {
                        return Enumerable.Empty<CompletionResult>();
                    }

                    string[] completeParts = wordToComplete.Split(s_typeSeparator, count: 2);
                    if (DslLoader.Instance.TryLoadDsl(completeParts[0], apiVersion, out ArmProviderDslInfo dslInfo))
                    {
                        foreach (string resourceType in dslInfo.ProviderSchema.Resources.Keys)
                        {
                            if (resourceType.StartsWith(completeParts[1], StringComparison.OrdinalIgnoreCase))
                            {
                                string fullCompletion = $"{completeParts[0]}/{resourceType}";
                                completions.Add(new CompletionResult(fullCompletion, fullCompletion, CompletionResultType.ParameterValue, fullCompletion));
                            }
                        }
                    }

                    return completions;
                }

                foreach (string schemaName in DslLoader.Instance.ListSchemaProviders(apiVersion))
                {
                    if (schemaName.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    {
                        string completion = $"{schemaName}/";
                        completions.Add(new CompletionResult(completion, completion, CompletionResultType.ParameterValue, completion));
                    }
                }

                return completions;
            }

            if (IsString(parameterName, "ApiVersion"))
            {
                string providerName = fakeBoundParameters.Contains("Type")
                    ? (string)fakeBoundParameters["Type"]
                    : null;

                var completions = new List<CompletionResult>();
                foreach (string apiVersion in DslLoader.Instance.ListSchemaVersions(providerName))
                {
                    if (apiVersion.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    {
                        completions.Add(new CompletionResult(apiVersion, apiVersion, CompletionResultType.ParameterValue, apiVersion));
                    }
                }

                return completions;
            }

            return null;
        }

        private static bool IsString(string str, string expected)
        {
            return string.Equals(str, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
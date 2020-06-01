
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
            if (IsString(parameterName, "Provider"))
            {
                var completions = new List<CompletionResult>();
                string apiVersion = fakeBoundParameters.Contains("ApiVersion")
                    ? (string)fakeBoundParameters["ApiVersion"]
                    : null;

                foreach (string providerName in DslLoader.Instance.ListSchemaProviders(apiVersion))
                {
                    if (providerName.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    {
                        completions.Add(new CompletionResult(providerName, providerName, CompletionResultType.ParameterValue, providerName));
                    }
                }

                return completions;
            }

            if (IsString(parameterName, "Type"))
            {
                var completions = new List<CompletionResult>();

                string apiVersion = (string)fakeBoundParameters["ApiVersion"];
                string provider = (string)fakeBoundParameters["Provider"];

                // We can't hope to load all schemas, if we have no version
                if (apiVersion == null || provider == null)
                {
                    return Enumerable.Empty<CompletionResult>();
                }

                if (DslLoader.Instance.TryLoadDsl(provider, apiVersion, out ArmProviderDslInfo dslInfo))
                {
                    foreach (string resourceType in dslInfo.ProviderSchema.Resources.Keys)
                    {
                        if (resourceType.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                        {
                            completions.Add(new CompletionResult(resourceType, resourceType, CompletionResultType.ParameterValue, resourceType));
                        }
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
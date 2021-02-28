
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using PSArm.Commands.Template;
using PSArm.Schema;
using PSArm.Schema.Keyword;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            ArmResourceName resourceName = GetResourceNameFromParameters(fakeBoundParameters);
            IEnumerable<string> completionStrings = ResourceKeywordSchema.Value.GetParameterValues(parameterName, resourceName.Namespace, resourceName.Type, resourceName.ApiVersion);

            if (!string.IsNullOrEmpty(wordToComplete))
            {
                completionStrings = completionStrings.Where(s => s.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase));
            }

            return GetCompletionResultsFromStrings(completionStrings);
        }

        private ArmResourceName GetResourceNameFromParameters(IDictionary fakeBoundParameters)
        {
            var provider = fakeBoundParameters[nameof(NewPSArmResourceCommand.Provider)] as string;
            var type = fakeBoundParameters[nameof(NewPSArmResourceCommand.Type)] as string;
            var apiVersion = fakeBoundParameters[nameof(NewPSArmResourceCommand.ApiVersion)] as string;

            return new ArmResourceName(provider, type, apiVersion);
        }

        private static IEnumerable<CompletionResult> GetCompletionResultsFromStrings(IEnumerable<string> stringValues)
        {
            var completions = new List<CompletionResult>();
            foreach (string str in stringValues)
            {
                completions.Add(new CompletionResult(str, str, CompletionResultType.ParameterValue, str));
            }
            return completions;
        }
    }
}
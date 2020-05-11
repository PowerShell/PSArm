using PSArm.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSArm.Completion
{
    public class ArmResourceArgumentCompleter : IArgumentCompleter
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
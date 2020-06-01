
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PSArm.Schema
{
    public class ArmDslProviderScriptProducer
    {
        private static readonly ArmDslStructureBuilder s_definitionStructureBuilder = new ArmDslStructureBuilder();

        private readonly ArmDslProviderSchema _providerSchema;

        private readonly Dictionary<string, string> _resourceDslDefinitions;

        private readonly Lazy<IReadOnlyDictionary<string, ArmDslKeywordDefinitionScope>> _resourceDslDefinitionsLazy;

        public ArmDslProviderScriptProducer(ArmDslProviderSchema providerSchema)
        {
            _providerSchema = providerSchema;
            _resourceDslDefinitions = new Dictionary<string, string>();
            _resourceDslDefinitionsLazy = new Lazy<IReadOnlyDictionary<string, ArmDslKeywordDefinitionScope>>(() => s_definitionStructureBuilder.GatherKeywordDefinitionStructure(_providerSchema));
        }

        public string ProviderName => _providerSchema.ProviderName;

        public string ProviderApiVersion => _providerSchema.ApiVersion;

        public string GetResourceScriptDefintion(string resourceName)
        {
            if (_resourceDslDefinitions.TryGetValue(resourceName, out string resourceDslDefinition))
            {
                return resourceDslDefinition;
            }

            if (_resourceDslDefinitionsLazy.Value.TryGetValue(resourceName, out ArmDslKeywordDefinitionScope dslDefinitionStructure))
            {
                resourceDslDefinition = GenerateResourceScriptDefinition(dslDefinitionStructure);
                _resourceDslDefinitions[resourceName] = resourceDslDefinition;
                return resourceDslDefinition;
            }

            throw new KeyNotFoundException($"No DSL entry for resource '{resourceName}' in provider '{ProviderName}', API version '{ProviderApiVersion}'");
        }

        private string GenerateResourceScriptDefinition(ArmDslKeywordDefinitionScope definitionStructure)
        {
            return new ArmDslResourceDefinitionWriter().WriteKeywordDefinitions(definitionStructure);
        }
    }

    public class ArmDslResourceDefinitionWriter
    {
        private readonly string _newline;

        private readonly StringBuilder _sb;

        private int _indent;

        public ArmDslResourceDefinitionWriter()
        {
            _sb = new StringBuilder();
            _newline = Environment.NewLine;
            _indent = 0;
        }

        public string WriteKeywordDefinitions(ArmDslKeywordDefinitionScope definitionStructure)
        {
            DoScopeDefinitions(definitionStructure);
            return _sb.ToString();
        }

        private void DoScopeDefinitions(ArmDslKeywordDefinitionScope scope)
        {
            foreach (KeyValuePair<ArmDslKeywordSchema, ArmDslKeywordDefinitionScope> keyword in scope.Keywords)
            {
                BeginKeywordDefinition(keyword.Key);

                if (keyword.Value != null)
                {
                    DoScopeDefinitions(keyword.Value);
                }

                EndKeywordDefinition(keyword.Key);

                _sb.Append(_newline);
                Newline();
                Newline();
            }
        }

        private void BeginKeywordDefinition(ArmDslKeywordSchema keyword)
        {
            _sb.Append("function ").Append(keyword.PSKeyword.Name);
            Newline();
            _sb.Append('{');
            _indent++;
            Newline();

            _sb.Append("param(");
            _indent++;
            Newline();

            bool wroteParameters = false;
            if (keyword.Parameters != null && keyword.Parameters.Count > 0)
            {
                wroteParameters = true;
                Intersperse(JoinParams, WriteParameter, keyword.Parameters.Values);
            }

            if (keyword.PropertyParameters != null && keyword.PropertyParameters.Count > 0)
            {
                if (wroteParameters)
                {
                    JoinParams();
                    Intersperse(JoinParams, WriteParameter, keyword.PropertyParameters.Values);
                }
                else if (keyword.PropertyParameters.Count == 1)
                {
                    WriteParameter(keyword.PropertyParameters.Values.First(), position: 0, mandatory: true);
                }
                else
                {
                    wroteParameters = true;
                    Intersperse(JoinParams, WriteParameter, keyword.PropertyParameters.Values);
                }
            }

            if (keyword.Body != null)
            {
                if (wroteParameters)
                {
                    JoinParams();
                }

                // Body written with position 0 since no other parameters are mandatory for body keywords
                WriteParameter("Body", "scriptblock", enums: null, position: 0, mandatory: false);
            }

            _indent--;
            Newline();
            _sb.Append(')');
            Newline();
            Newline();
        }

        private void EndKeywordDefinition(ArmDslKeywordSchema keyword)
        {
            WriteFunctionImplementation(keyword);

            _indent--;
            Newline();
            _sb.Append('}');
        }

        private void WriteFunctionImplementation(ArmDslKeywordSchema keyword)
        {
            // TODO: Differentiate propertyParameters from parameters and adapt PS cmdlets to take this input

            if (keyword.Array)
            {
                WriteParameterAssignments(keyword.PSKeyword, out string parameterVar, out string propertyVar);
                _sb.Append("ArrayItem ");
                WriteLiteral(keyword.Name);

                WriteParameterPassingParameters(parameterVar, propertyVar);

                if (keyword.Body != null)
                {
                    _sb.Append(' ');
                    WriteVariable("Body");
                }

                return;
            }

            if (keyword.Body != null)
            {
                WriteParameterAssignments(keyword.PSKeyword, out string parameterVar, out string propertyVar);

                _sb.Append("Block ");
                WriteLiteral(keyword.Name);

                WriteParameterPassingParameters(parameterVar, propertyVar);

                _sb.Append(' ');
                WriteVariable("Body");
                return;
            }

            if (keyword.PropertyParameters.Count == 1)
            {
                _sb.Append("Value ");
                WriteLiteral(keyword.Name);
                _sb.Append(' ');
                WriteVariable(keyword.PropertyParameters.Values.First().Name);
                return;
            }

            WriteParameterAssignments(keyword.PSKeyword, out string paramsVar, out string propsVar);

            _sb.Append("Composite ");
            WriteLiteral(keyword.Name);

            WriteParameterPassingParameters(paramsVar, propsVar);
        }

        private void WriteParameterPassingParameters(string parametersVariable, string propertyParametersVariable)
        {
            if (parametersVariable != null)
            {
                _sb.Append(" -Parameters ");
                WriteVariable(parametersVariable);
            }

            if (propertyParametersVariable != null)
            {
                _sb.Append(" -Properties ");
                WriteVariable(propertyParametersVariable);
            }
        }

        private void WriteParameterAssignments(PSDslKeyword keyword, out string parametersVariable, out string propertyParametersVariable)
        {
            parametersVariable = null;
            propertyParametersVariable = null;

            if (keyword.HasParameters)
            {
                parametersVariable = "KwParameters";
                WriteVariable(parametersVariable);
                _sb.Append(" = ");
                WriteParameterHashtable(keyword.Parameters);
            }

            if (keyword.HasProperties)
            {
                propertyParametersVariable = "KwProperties";
                WriteVariable(propertyParametersVariable);
                _sb.Append(" = ");
                WriteParameterHashtable(keyword.Parameters, isPropertyParameter: true);
            }
        }

        private void WriteParameterHashtable(IReadOnlyDictionary<string, PSDslParameterInfo> parameters, bool isPropertyParameter = false)
        {
            _sb.Append("@{");
            _indent++;
            Newline();

            bool needNewline = false;
            foreach (KeyValuePair<string, PSDslParameterInfo> parameter in parameters)
            {
                if (parameter.Value.IsPropertyParameter != isPropertyParameter)
                {
                    continue;
                }

                if (needNewline)
                {
                    Newline();
                }

                WriteParameterHashtableEntry(parameter);
                needNewline = true;
            }

            _indent--;
            Newline();
            _sb.Append('}');
            Newline();
            Newline();
        }

        private void WriteParameterHashtableEntry(KeyValuePair<string, PSDslParameterInfo> parameter)
        {
            _sb.Append(parameter.Value.Parameter.Name).Append(" = ");
            _sb.Append("if ($PSBoundParameters.ContainsKey(");
            WriteLiteral(parameter.Key);
            _sb.Append(")) { ");
            WriteVariable(parameter.Key);
            _sb.Append(" } else { ");
            WriteLiteral(null);
            _sb.Append(" }");
        }

        private void Newline()
        {
            _sb.Append(_newline).Append(' ', 4 * _indent);
        }

        private void WriteParameter(ArmDslParameterSchema parameter) => WriteParameter(parameter, position: null, mandatory: false);

        private void WriteParameter(ArmDslParameterSchema parameter, int? position, bool mandatory)
            => WriteParameter(parameter.Name, parameter.Type, parameter.Enum, position, mandatory);

        private void WriteParameter(string name, string type, IReadOnlyList<object> enums, int? position, bool mandatory)
        {
            _sb.Append("[Parameter(");

            bool needComma = false;

            if (mandatory)
            {
                needComma = true;
                _sb.Append("Mandatory");
            }

            if (position != null)
            {
                if (needComma)
                {
                    _sb.Append(", ");
                }

                _sb.Append("Position = ").Append(position.Value);
            }

            // End parameter
            _sb.Append(")]");

            Newline();

            if (enums != null)
            {
                _sb.Append("[ValidateSet(");
                Intersperse(JoinArray, WriteLiteral, enums);
                _sb.Append(")]");
                Newline();
            }

            if (type != null)
            {
                _sb.Append('[').Append(type).Append(']');
                Newline();
            }

            WriteVariable(name);
        }

        private void WriteLiteral(object value)
        {
            switch (value)
            {
                case null:
                    _sb.Append("$null");
                    return;

                case string s:
                    _sb.Append('\'').Append(s.Replace("'", "''")).Append('\'');
                    return;

                case bool b:
                    _sb.Append(b ? "$true" : "$false");
                    return;

                case int i:
                    _sb.Append(i);
                    return;

                case long l:
                    _sb.Append(l);
                    return;

                case double d:
                    _sb.Append(d);
                    return;

                default:
                    throw new NotImplementedException();
            }
        }

        private void WriteVariable(string variableName)
        {
            _sb.Append('$').Append(variableName);
        }

        private void JoinArray()
        {
            _sb.Append(", ");
        }

        private void JoinParams()
        {
            _sb.Append(',');
            Newline();
            Newline();
        }

        private void Intersperse<T>(Action join, Action<T> writeItem, IReadOnlyCollection<T> items)
        {
            T[] arr = items.ToArray();

            if (arr.Length == 0)
            {
                return;
            }

            writeItem(arr[0]);

            for (int i = 1; i < arr.Length; i++)
            {
                join();
                writeItem(arr[i]);
            }
        }
    }
}

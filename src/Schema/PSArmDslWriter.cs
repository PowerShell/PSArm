using Azure.Bicep.Types.Concrete;
using PSArm.Commands.Primitive;
using PSArm.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace PSArm.Schema
{
    internal class PSArmDslWriter
    {
        private const string KeywordValueParameter = "Value";

        private const string KeywordBodyParameter = "Body";

        private static readonly char[] s_badPowerShellPrefixes = new[] { '@' };

        private readonly PowerShellWriter _writer;

        private readonly Stack<ScopedKeyword> _keywordsInScope;

        public PSArmDslWriter(TextWriter textWriter)
        {
            _writer = new PowerShellWriter(textWriter);
            _keywordsInScope = new Stack<ScopedKeyword>();
        }

        public void WriteSchemaDefinition(string keyword, TypeBase resourceSchema, out string sanitizedKeyword)
        {
            sanitizedKeyword = SanitizeKeywordForFunctionName(keyword);
            _keywordsInScope.Push(new ScopedKeyword(sanitizedKeyword, resourceSchema));
            WriteKeywordDefinitionBody(armKey: keyword, resourceSchema, arrayDepth: 0);
            _keywordsInScope.Pop();
        }

        private void WriteKeywordDefinition(string keyword, TypeBase schema, int arrayDepth = 0)
        {
            string sanitizedKeyword = SanitizeKeywordForFunctionName(keyword);

            if (IsKeywordAlreadyDefinedInScope(sanitizedKeyword, schema))
            {
                return;
            }

            _keywordsInScope.Push(new ScopedKeyword(sanitizedKeyword, schema));

            _writer.OpenFunction(sanitizedKeyword);

            // We use the unsanitized keyword here since that is what must be written into the JSON.
            // The function body doesn't need to know what the sanitized keyword is
            WriteKeywordDefinitionBody(armKey: keyword, schema, arrayDepth);

            _writer.CloseFunction();

            _keywordsInScope.Pop();
        }

        private void WriteKeywordDefinitionBody(string armKey, TypeBase schema, int arrayDepth)
        {
            switch (schema)
            {
                case ArrayType array:
                    WriteKeywordDefinitionBody(armKey, array, arrayDepth);
                    return;

                case BuiltInType builtin:
                    WriteKeywordDefinitionBody(armKey, builtin, arrayDepth);
                    return;

                case DiscriminatedObjectType discriminatedType:
                    WriteKeywordDefinitionBody(armKey, discriminatedType, arrayDepth);
                    return;

                case ObjectType objectType:
                    WriteKeywordDefinitionBody(armKey, objectType, arrayDepth);
                    return;

                case ResourceType resource:
                    WriteKeywordDefinitionBody(armKey, resource, arrayDepth);
                    return;

                case StringLiteralType stringLiteral:
                    WriteKeywordDefinitionBody(armKey, stringLiteral, arrayDepth);
                    return;

                case UnionType union:
                    WriteKeywordDefinitionBody(armKey, union, arrayDepth);
                    return;

                default:
                    throw new ArgumentException($"Unknown resource schema type: {schema.GetType()}");
            }
        }

        private void WriteKeywordDefinitionBody(string armKey, ArrayType array, int arrayDepth)
        {
            WriteKeywordDefinitionBody(armKey, array.ItemType.Type, arrayDepth + 1);
        }

        private void WriteKeywordDefinitionBody(string armKey, DiscriminatedObjectType discriminatedType, int arrayDepth)
        {
            // For discriminated objects, we assume we basically have a base object
            // and a list of conceptual "subclasses" each with an associated discriminator.
            // We solve this as follows:
            //  - Add the "discriminator" as a mandatory enum parameter
            //  - Define the functions for the base properties
            //  - Conditionally define the subclass properties
            //  - Invoke the call as normal

            string discriminatorName = SanitizeStringForVariableName(discriminatedType.Discriminator);

            WriteKeywordBodyParamBlock(discriminatorName, (IReadOnlyCollection<string>)discriminatedType.Elements.Keys);

            WriteKeywordNestedFunctionDefinitions(discriminatedType, discriminatorName);

            WritePrimitiveInvocation(
                armKey,
                KeywordArgumentKind.Body,
                arrayDepth,
                discriminatorKey: discriminatedType.Discriminator,
                discriminatorVariable: discriminatorName);
        }

        private void WriteKeywordDefinitionBody(string armKey, ObjectType objectType, int arrayDepth)
        {
            WriteKeywordBodyParamBlock();

            WriteKeywordNestedFunctionDefinitions(objectType);

            WritePrimitiveInvocation(armKey, KeywordArgumentKind.Body, arrayDepth);
        }

        private void WriteKeywordDefinitionBody(string armKey, BuiltInType builtin, int arrayDepth)
        {
            WriteKeywordValueParamBlock(builtin.Kind);

            WritePrimitiveInvocation(armKey, KeywordArgumentKind.Value, arrayDepth);
        }

        private void WriteKeywordDefinitionBody(string armKey, ResourceType resource, int arrayDepth)
        {
            throw new InvalidOperationException($"Cannot define a keyword for resource types");
        }

        private void WriteKeywordDefinitionBody(string armKey, StringLiteralType stringLiteral, int arrayDepth)
        {
            WriteKeywordValueParamBlock(enumeratedValues: new string[] { stringLiteral.Value });
            WritePrimitiveInvocation(armKey, KeywordArgumentKind.Value, arrayDepth);
        }

        private void WriteKeywordDefinitionBody(string armKey, UnionType union, int arrayDepth)
        {
            IReadOnlyList<string> enumValues = GetEnumValuesFromUnionElements(union.Elements);
            WriteKeywordValueParamBlock(enumeratedValues: enumValues);
            WritePrimitiveInvocation(armKey, KeywordArgumentKind.Value, arrayDepth);
        }

        private void WriteKeywordBodyParamBlock(
            string discriminatorName = null,
            IReadOnlyCollection<string> discriminatorValues = null)
        {
            _writer.OpenParamBlock()
                    .OpenAttribute("Parameter")
                        .Write("Position = 0, Mandatory = $true")
                        .CloseAttribute()
                        .WriteLine()
                    .WriteType("scriptblock")
                        .WriteLine()
                    .WriteVariable(KeywordBodyParameter);

            if (discriminatorName != null
                && discriminatorValues != null)
            {
                // TODO: Move from ValidateSet to an ARM-enlightened validation function
                _writer
                    .Write(",")
                    .WriteLine(lineCount: 2)
                    .OpenAttribute("Parameter")
                        .Write("Mandatory = $true")
                        .CloseAttribute()
                        .WriteLine()
                    .OpenAttribute("ValidateSet")
                        .Intersperse(
                            entry => _writer.WriteValue(entry),
                            ", ",
                            discriminatorValues)
                        .CloseAttribute()
                        .WriteLine()
                    .WriteType("string")
                        .WriteLine()
                    .WriteVariable(discriminatorName);
            }

            _writer.CloseParamBlock().WriteLine();
        }

        private void WriteKeywordValueParamBlock(
            BuiltInTypeKind? builtinTypeKind = null,
            IReadOnlyList<string> enumeratedValues = null)
        {
            _writer
                .OpenParamBlock()
                    .OpenAttribute("Parameter")
                        .Write("Position = 0, Mandatory = $true")
                        .CloseAttribute()
                        .WriteLine();

            if (enumeratedValues != null)
            {
                // TODO: Move from ValidateSet to an ARM-enlightened validation function
                _writer
                    .OpenAttribute("ValidateSet")
                        .Intersperse(
                            e => _writer.WriteValue(e),
                            ", ",
                            enumeratedValues)
                        .CloseAttribute()
                        .WriteLine();
            }

            if (builtinTypeKind != null
                && TryGetTypeNameForBuiltin(builtinTypeKind.Value, out string typeName))
            {
                _writer
                    .WriteType(typeName)
                        .WriteLine();
            }

            _writer
                .WriteVariable(KeywordValueParameter)
                .CloseParamBlock()
                .WriteLine();
        }

        private void WritePrimitiveInvocation(
            string keyword,
            KeywordArgumentKind argumentKind,
            int arrayDepth,
            string discriminatorKey = null,
            string discriminatorVariable = null)
        {
            switch (arrayDepth)
            {
                case 0:
                    WritePrimitiveInvocation(keyword, argumentKind, discriminatorKey: discriminatorKey, discriminatorVariable: discriminatorVariable);
                    return;

                case 1:
                    WritePrimitiveInvocation(keyword, argumentKind, KeywordArrayKind.Entry, discriminatorKey, discriminatorVariable);
                    return;

                default:
                    WritePrimitiveInvocation(keyword, KeywordArgumentKind.Body, KeywordArrayKind.NestedBody, discriminatorKey, discriminatorVariable);
                    return;
            }
        }

        private void WritePrimitiveInvocation(
            string keyword,
            KeywordArgumentKind argumentKind,
            KeywordArrayKind arrayKind = KeywordArrayKind.None,
            string discriminatorKey = null,
            string discriminatorVariable = null)
        {
            _writer
                .WriteCommand(NewPSArmEntryCommand.KeywordName)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Key))
                        .WriteSpace()
                        .WriteValue(keyword);

            if (discriminatorKey is not null)
            {
                _writer
                    .WriteParameter(nameof(NewPSArmEntryCommand.DiscriminatorKey))
                        .WriteSpace()
                        .WriteValue(discriminatorKey)
                    .WriteParameter(nameof(NewPSArmEntryCommand.DiscriminatorValue))
                        .WriteSpace()
                        .WriteVariable(discriminatorVariable);
            }

            switch (argumentKind)
            {
                case KeywordArgumentKind.Value:
                    _writer
                        .WriteParameter(nameof(NewPSArmEntryCommand.Value))
                            .WriteSpace()
                            .WriteVariable(KeywordValueParameter);
                    break;

                case KeywordArgumentKind.Body:
                    _writer
                        .WriteParameter(nameof(NewPSArmEntryCommand.Body))
                            .WriteSpace()
                            .WriteVariable(KeywordBodyParameter);
                    break;
            }

            switch (arrayKind)
            {
                case KeywordArrayKind.Entry:
                    _writer.WriteParameter(nameof(NewPSArmEntryCommand.Array));
                    break;

                case KeywordArrayKind.NestedBody:
                    _writer.WriteParameter(nameof(NewPSArmEntryCommand.ArrayBody));
                    break;
            }
        }

        private void WriteKeywordNestedFunctionDefinitions(DiscriminatedObjectType discriminatedType, string discriminatorName)
        {
            // Define the body and discriminator parameters
            // Define all the common base properties as functions
            bool needNewline = false;
            foreach (KeyValuePair<string, ObjectProperty> baseProperty in discriminatedType.BaseProperties)
            {
                if (needNewline)
                {
                    _writer.WriteLine();
                }

                WriteKeywordDefinition(baseProperty.Key, baseProperty.Value.Type.Type);

                needNewline = true;
            }

            WriteDiscriminatedKeywordDefinitions(discriminatorName, discriminatedType.Discriminator, (IReadOnlyDictionary<string, ITypeReference>)discriminatedType.Elements);
        }

        private void WriteDiscriminatedKeywordDefinitions(string discriminatorVariable, string discriminatorKey, IReadOnlyDictionary<string, ITypeReference> discriminatedElements)
        {
            // Conditionally define the functions in a switch statement
            if (discriminatedElements.Count == 0)
            {
                return;
            }

            _writer
                .WriteLine()
                .Write("switch (")
                    .WriteVariable(discriminatorVariable)
                    .Write(")")
                .OpenBlock();

            bool needNewline = false;
            foreach (KeyValuePair<string, ITypeReference> discriminatedSchemaEntry in discriminatedElements)
            {
                if (needNewline)
                {
                    _writer.WriteLine();
                }

                TypeBase discriminatedSchema = discriminatedSchemaEntry.Value.Type;
                switch (discriminatedSchema)
                {
                    case ObjectType objectType:
                        if (objectType.Properties.Count == 0)
                        {
                            continue;
                        }

                        _writer.WriteValue(discriminatedSchemaEntry.Key)
                            .OpenBlock();

                        foreach (KeyValuePair<string, ObjectProperty> discriminatedProperty in objectType.Properties)
                        {
                            if (discriminatedProperty.Key.Equals(discriminatorKey))
                            {
                                continue;
                            }

                            if (needNewline)
                            {
                                _writer.WriteLine();
                            }

                            WriteKeywordDefinition(discriminatedProperty.Key, discriminatedProperty.Value.Type.Type);

                            needNewline = true;
                        }

                        _writer
                            .WriteLine()
                            .Write("break")
                            .CloseBlock();
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported discriminated schema entry of type '{discriminatedSchema.GetType()}'");
                }

                needNewline = true;
            }

            _writer
                .CloseBlock()
                .WriteLine();
        }

        private void WriteKeywordNestedFunctionDefinitions(ObjectType objectType)
        {
            foreach (KeyValuePair<string, ObjectProperty> entry in objectType.Properties)
            {
                WriteKeywordDefinition(entry.Key, entry.Value.Type.Type);
                _writer.WriteLine(lineCount: 2);
            }

            // TODO: Handle additional properties more intelligently when the type is not ObjectType
            if (TryGetObjectSchema(objectType.AdditionalProperties, out ObjectType additionalProperties))
            {
                // TODO: Ensure the additional properties type doesn't itself have additional properties...

                foreach (KeyValuePair<string, ObjectProperty> entry in additionalProperties.Properties)
                {
                    WriteKeywordDefinition(entry.Key, entry.Value.Type.Type);
                    _writer.WriteLine(lineCount: 2);
                }
            }
        }


        private IReadOnlyList<string> GetEnumValuesFromUnionElements(IReadOnlyList<ITypeReference> elementTypes)
        {
            var values = new string[elementTypes.Count];

            for (int i = 0; i < elementTypes.Count; i++)
            {
                ITypeReference elementType = elementTypes[i];
                switch (elementType.Type)
                {
                    case StringLiteralType str:
                        values[i] = str.Value;
                        continue;

                    default:
                        throw new ArgumentException($"Unsupported union type '{elementType.Type.GetType()}'");
                }
            }

            return values;
        }

        private bool TryGetObjectSchema(ITypeReference typeRef, out ObjectType objectType) => TryGetObjectSchema(typeRef?.Type, out objectType);

        private bool TryGetObjectSchema(TypeBase type, out ObjectType objectType)
        {
            if (type is null
                || type is not ObjectType obj)
            {
                objectType = null;
                return false;
            }

            objectType = obj;
            return true;
        }

        private bool TryGetTypeNameForBuiltin(BuiltInTypeKind kind, out string typeName)
        {
            switch (kind)
            {
                case BuiltInTypeKind.Array:
                    typeName = "array";
                    return true;

                case BuiltInTypeKind.Bool:
                    typeName = "bool";
                    return true;

                case BuiltInTypeKind.Int:
                    typeName = "int";
                    return true;

                case BuiltInTypeKind.String:
                    typeName = "string";
                    return true;

                default:
                    typeName = null;
                    return false;
            }
        }

        private bool IsKeywordAlreadyDefinedInScope(string keyword, TypeBase schema)
        {
            foreach (ScopedKeyword scopedKeyword in _keywordsInScope)
            {
                if (scopedKeyword.Keyword.Equals(keyword)
                    && scopedKeyword.Schema.Equals(schema))
                {
                    return true;
                }
            }

            return false;
        }

        private string SanitizeKeywordForFunctionName(string s)
        {
            return s.Trim(s_badPowerShellPrefixes);
        }

        private string SanitizeStringForVariableName(string s)
        {
            return s.Trim(s_badPowerShellPrefixes).Replace('.', '_');
        }

        private readonly struct ScopedKeyword
        {
            public ScopedKeyword(
                string keyword,
                TypeBase schema)
            {
                Keyword = keyword;
                Schema = schema;
            }

            public readonly string Keyword;

            public readonly TypeBase Schema;
        }

        private enum KeywordArgumentKind
        {
            Value,
            Body,
        }

        private enum KeywordArrayKind
        {
            None,
            Entry,
            NestedBody,
        }
    }
}

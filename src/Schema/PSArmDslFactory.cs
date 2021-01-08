using Azure.Bicep.Types.Concrete;
using PSArm.Commands.Primitive;
using PSArm.Serialization;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace PSArm.Schema
{
    public class PSArmDslFactory
    {
        public Dictionary<string, ScriptBlock> CreateResourceDslContext(
            IReadOnlyDictionary<string, TypeBase> resourceSchema,
            string discriminator,
            IReadOnlyDictionary<string, ITypeReference> discriminatedSubtypes)
        {
            var functionDictionary = new Dictionary<string, ScriptBlock>();

            foreach (KeyValuePair<string, TypeBase> propertySchemaEntry in resourceSchema)
            {
                using (var sw = new StringWriter())
                {
                    var writer = new PSArmDslWriter(sw);
                    writer.WriteSchemaDefinition(propertySchemaEntry.Key, propertySchemaEntry.Value);
                    functionDictionary[propertySchemaEntry.Key] = ScriptBlock.Create(sw.ToString());
                }
            }

            // TODO: Implemente discriminated properties
            // - Ensure a discriminator parameter is available on the resource command
            // - Define a function that conditionally defines the correct keywords depending on the discriminator and add it to the context table here
            // - Dot-source that function with the discriminator parameter when the resource is invoked

            return functionDictionary;
        }
    }

    internal class PSArmDslWriter
    {
        private const string BodyKeywordBodyParameter = "Body";

        private static readonly char[] s_badFunctionPrefixes = new[] { '@' };

        private readonly PowerShellWriter _writer;

        private readonly Stack<ScopedKeyword> _keywordsInScope;

        public PSArmDslWriter(TextWriter textWriter)
        {
            _writer = new PowerShellWriter(textWriter);
            _keywordsInScope = new Stack<ScopedKeyword>();
        }

        public void WriteSchemaDefinition(string keyword, TypeBase resourceSchema)
        {
            string sanitizedKeyword = keyword.Trim(s_badFunctionPrefixes);
            _keywordsInScope.Push(new ScopedKeyword(sanitizedKeyword, resourceSchema));
            WriteKeywordDefinitionBody(sanitizedKeyword, resourceSchema, forArray: false);
            _keywordsInScope.Pop();
        }

        private void WriteKeywordDefinition(string keyword, TypeBase schema, bool forArray = false)
        {
            string sanitizedKeyword = keyword.Trim(s_badFunctionPrefixes);

            if (IsKeywordAlreadyDefinedInScope(sanitizedKeyword, schema))
            {
                return;
            }

            _keywordsInScope.Push(new ScopedKeyword(sanitizedKeyword, schema));

            _writer.OpenFunction(sanitizedKeyword);

            WriteKeywordDefinitionBody(sanitizedKeyword, schema, forArray);

            _writer.CloseFunction();

            _keywordsInScope.Pop();
        }

        private void WriteKeywordDefinitionBody(string keyword, TypeBase schema, bool forArray)
        {
            switch (schema)
            {
                case ArrayType array:
                    WriteKeywordDefinitionBody(keyword, array, forArray);
                    return;

                case BuiltInType builtin:
                    WriteKeywordDefinitionBody(keyword, builtin, forArray);
                    return;

                case DiscriminatedObjectType discriminatedType:
                    WriteKeywordDefinitionBody(keyword, discriminatedType, forArray);
                    return;

                case ObjectType objectType:
                    WriteKeywordDefinitionBody(keyword, objectType, forArray);
                    return;

                case ResourceType resource:
                    WriteKeywordDefinitionBody(keyword, resource, forArray);
                    return;

                case StringLiteralType stringLiteral:
                    WriteKeywordDefinitionBody(keyword, stringLiteral, forArray);
                    return;

                case UnionType union:
                    WriteKeywordDefinitionBody(keyword, union, forArray);
                    return;

                default:
                    throw new ArgumentException($"Unknown resource schema type: {schema.GetType()}");
            }
        }

        private void WriteKeywordDefinitionBody(string keyword, ArrayType array, bool forArray)
        {
            if (forArray)
            {
                WriteArrayBodyKeywordDefinition(keyword, array.ItemType.Type, nestingDepth: 2);
                return;
            }

            WriteKeywordDefinitionBody(keyword, array.ItemType.Type, forArray: true);
        }

        private void WriteKeywordDefinitionBody(string keyword, DiscriminatedObjectType discriminatedType, bool forArray)
        {
            // For discriminated objects, we assume we basically have a base object
            // and a list of conceptual "subclasses" each with an associated discriminator.
            // We solve this as follows:
            //  - Add the "discriminator" as a mandatory enum parameter
            //  - Define the functions for the base properties
            //  - Conditionally define the subclass properties
            //  - Invoke the call as normal

            // Define the body and discriminator parameters
            _writer.OpenParamBlock()
                .OpenAttribute("Parameter")
                        .Write("Mandatory = $true, Position = 0")
                        .CloseAttribute()
                        .WriteLine()
                    .WriteType("scriptblock")
                        .WriteLine()
                    .WriteVariable(BodyKeywordBodyParameter)
                    .Write(",")
                    .WriteLine()
                .OpenAttribute("Parameter")
                        .Write("Mandatory = $true")
                        .CloseAttribute()
                        .WriteLine()
                    .OpenAttribute("ValidateSet")
                        .Intersperse(
                            entry => _writer.Write(entry),
                            ", ",
                            (IReadOnlyCollection<string>)discriminatedType.Elements.Keys)
                        .CloseAttribute()
                        .WriteLine()
                    .WriteType("string")
                        .WriteLine()
                    .WriteVariable(discriminatedType.Discriminator)
                .CloseParamBlock()
                .WriteLine();

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

            // Conditionally define the functions in a switch statement
            _writer
                .Write("switch (")
                    .WriteVariable(discriminatedType.Discriminator)
                    .Write(")")
                .OpenBlock();

            needNewline = false;
            foreach (KeyValuePair<string, ITypeReference> discriminatedSchemaEntry in discriminatedType.Elements)
            {
                if (needNewline)
                {
                    _writer.WriteLine();
                }

                _writer.WriteValue(discriminatedSchemaEntry.Key)
                    .OpenBlock();

                TypeBase discriminatedSchema = discriminatedSchemaEntry.Value.Type;
                switch (discriminatedSchema)
                {
                    case ObjectType objectType:
                        foreach (KeyValuePair<string, ObjectProperty> discriminatedProperty in objectType.Properties)
                        {
                            if (discriminatedProperty.Key.Equals(discriminatedType.Discriminator))
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
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported discriminated schema entry of type '{discriminatedSchema.GetType()}'");
                }

                _writer.CloseBlock();

                needNewline = true;
            }

            _writer
                .CloseBlock()
                .WriteLine();

            _writer
                .WriteCommand(NewPSArmEntryCommand.Name)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Key))
                        .WriteSpace()
                        .WriteValue(keyword)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Body))
                        .WriteSpace()
                        .WriteVariable(BodyKeywordBodyParameter);
        }

        private void WriteKeywordDefinitionBody(string keyword, ObjectType objectType, bool forArray)
        {
            _writer.OpenParamBlock()
                    .OpenAttribute("Parameter")
                        .Write("Position = 0, Mandatory = $true")
                        .CloseAttribute()
                        .WriteLine()
                    .WriteType("scriptblock")
                        .WriteLine()
                    .WriteVariable(BodyKeywordBodyParameter)
                .CloseParamBlock()
                .WriteLine();

            foreach (KeyValuePair<string, ObjectProperty> entry in objectType.Properties)
            {
                WriteKeywordDefinition(entry.Key, entry.Value.Type.Type);
                _writer.WriteLine(lineCount: 2);
            }

            _writer
                .WriteCommand(NewPSArmEntryCommand.Name)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Key))
                        .WriteSpace()
                        .WriteValue(keyword)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Body))
                        .WriteSpace()
                        .WriteVariable(BodyKeywordBodyParameter);
        }

        private void WriteKeywordDefinitionBody(string keyword, BuiltInType builtin, bool forArray)
        {
            _writer
                .OpenParamBlock()
                    .OpenAttribute("Parameter")
                        .Write("Mandatory = $true, Position = 0")
                        .CloseAttribute()
                        .WriteLine();

            if (TryGetTypeNameForBuiltin(builtin.Kind, out string typeName))
            {
                _writer
                    .WriteType(typeName)
                    .WriteLine();
            }

            _writer
                .WriteVariable("Value")
                .CloseParamBlock()
                .WriteLine();

            _writer
                .WriteCommand(NewPSArmEntryCommand.Name)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Key))
                        .WriteSpace()
                        .WriteValue(keyword)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Value))
                        .WriteSpace()
                        .WriteVariable("Value");

            if (forArray)
            {
                _writer.WriteParameter(nameof(NewPSArmEntryCommand.Array));
            }
        }

        private void WriteKeywordDefinitionBody(string keyword, ResourceType resource, bool forArray)
        {
            throw new InvalidOperationException($"Cannot define a keyword for resource types");
        }

        private void WriteKeywordDefinitionBody(string keyword, StringLiteralType stringLiteral, bool forArray)
        {
            WriteKeywordDefinitionBodyForEnumValues(keyword, new string[] { stringLiteral.Value }, forArray);
        }

        private void WriteKeywordDefinitionBody(string keyword, UnionType union, bool forArray)
        {
            IReadOnlyList<string> enumValues = GetEnumValuesFromUnionElements(union.Elements);
            WriteKeywordDefinitionBodyForEnumValues(keyword, enumValues, forArray);
        }

        private void WriteKeywordDefinitionBodyForEnumValues(string keyword, IReadOnlyList<string> enumValues, bool forArray)
        {
            // TODO: Move from ValidateSet to an ARM-enlightened validation function

            _writer
                .OpenParamBlock()
                    .OpenAttribute("Parameter")
                        .Write("Mandatory = $true, Position = 0")
                        .CloseAttribute()
                        .WriteLine()
                    .OpenAttribute("ValidateSet")
                        .Intersperse(
                            (s) => _writer.WriteValue(s),
                            ", ",
                            enumValues)
                        .CloseAttribute()
                        .WriteLine()
                    .WriteType("string")
                        .WriteLine()
                    .WriteVariable("Value")
                    .CloseParamBlock()
                .WriteLine()
                .WriteCommand(NewPSArmEntryCommand.Name)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Key))
                        .WriteSpace()
                        .WriteValue(keyword)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Value))
                        .WriteSpace()
                        .WriteVariable("Value");

            if (forArray)
            {
                _writer.WriteParameter(nameof(NewPSArmEntryCommand.Array));
            }
        }

        private void WriteKeywordNestedFunctionDefinitions(ObjectType objectSchema)
        {

        }

        private void WriteKeywordNestedFunctionDefinitions(DiscriminatedObjectType discriminatedSchema)
        {

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
    }

    public static class SchemaTest
    {
        public static void Run()
        {
            var index = new ResourceIndex();
            var factory = new PSArmDslFactory();
            foreach (ResourceSchema schema in index.GetResourceSchemas())
            {
                factory.CreateResourceDslContext(schema.Properties, schema.Discriminator, schema.DiscriminatedSubtypes);
            }
        }
    }
}

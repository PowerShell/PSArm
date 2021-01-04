using Azure.Bicep.Types.Concrete;
using PSArm.Commands.Primitive;
using PSArm.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace PSArm.Schema
{
    public class PSArmDslFactory
    {
        public Dictionary<string, ScriptBlock> CreateResourceDslContext(IReadOnlyDictionary<string, TypeBase> resourceSchema)
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

            return functionDictionary;
        }
    }

    internal class PSArmDslWriter
    {
        private const string BodyKeywordBodyParameter = "Body";

        private readonly PowerShellWriter _writer;

        private readonly Stack<ScopedKeyword> _keywordsInScope;

        public PSArmDslWriter(TextWriter textWriter)
        {
            _writer = new PowerShellWriter(textWriter);
            _keywordsInScope = new Stack<ScopedKeyword>();
        }

        public void WriteSchemaDefinition(string keyword, TypeBase resourceSchema)
            => WriteKeywordDefinitionBody(keyword, resourceSchema, forArray: false);

        private void WriteKeywordDefinition(string keyword, TypeBase schema, bool forArray = false)
        {
            _writer.OpenFunction(keyword);

            WriteKeywordDefinitionBody(keyword, schema, forArray);

            _writer.CloseFunction();
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
                throw new InvalidOperationException($"Unsupported nested array type for keyword '{keyword}'");
            }

            WriteKeywordDefinition(keyword, array.ItemType.Type, forArray: true);
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
        }

        private void WriteKeywordDefinitionBody(string keyword, ObjectType objectType, bool forArray)
        {
            _writer.OpenParamBlock()
                    .OpenAttribute("Parameter")
                        .Write("Position = 0, Mandatory = $true")
                        .CloseAttribute()
                        .WriteLine()
                    .WriteType("scriptblock")
                    .WriteVariable(BodyKeywordBodyParameter)
                .CloseParamBlock()
                .WriteLine();

            _keywordsInScope.Push(new ScopedKeyword(keyword, objectType));
            foreach (KeyValuePair<string, ObjectProperty> entry in objectType.Properties)
            {
                WriteKeywordDefinition(entry.Key, entry.Value.Type.Type);
                _writer.WriteLine(lineCount: 2);
            }

            _writer
                .WriteCommand(NewPSArmEntryCommand.Name)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Key))
                        .WriteValue(keyword)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Body))
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
                        .Write(" ")
                        .WriteValue(keyword)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Value))
                        .Write(" ")
                        .WriteVariable("Value");

            if (forArray)
            {
                _writer.WriteParameter(nameof(NewPSArmEntryCommand.Array));
            }
        }

        private void WriteKeywordDefinitionBody(string keyword, ResourceType resource, bool forArray)
        {

        }

        private void WriteKeywordDefinitionBody(string keyword, StringLiteralType stringLiteral, bool forArray)
        {

        }

        private void WriteKeywordDefinitionBody(string keyword, UnionType union, bool forArray)
        {
            IReadOnlyList<string> enumValues = GetEnumValuesFromUnionElements(union.Elements);

            _writer
                .OpenParamBlock()
                    .OpenAttribute("Parameter")
                        .Write("Mandatory = $true, Position = 0")
                        .CloseAttribute()
                    .OpenAttribute("ValidateSet")
                        .Intersperse(
                            (s) => _writer.WriteValue(s),
                            () => _writer.Write(", "),
                            enumValues)
                        .CloseAttribute()
                    .WriteType("string")
                    .WriteVariable("Value")
                    .CloseParamBlock()
                .WriteLine()
                .WriteCommand(NewPSArmEntryCommand.Name)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Key))
                        .Write(" ")
                        .WriteValue(keyword)
                    .WriteParameter(nameof(NewPSArmEntryCommand.Value))
                        .Write(" ")
                        .WriteVariable("Value");

            if (forArray)
            {
                _writer.WriteParameter(nameof(NewPSArmEntryCommand.Array));
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
                factory.CreateResourceDslContext(schema.Properties);
            }
        }
    }
}

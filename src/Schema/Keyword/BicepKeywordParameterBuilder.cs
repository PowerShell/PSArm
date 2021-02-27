
// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using PSArm.Internal;
using PSArm.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class BicepKeywordParameterBuilder : BicepArmTypeVisitor<IReadOnlyDictionary<string, DslParameterInfo>>
    {
        private static readonly ConcurrentDictionary<BuiltInTypeKind, IReadOnlyDictionary<string, DslParameterInfo>> s_builtinParameters =
            new ConcurrentDictionary<BuiltInTypeKind, IReadOnlyDictionary<string, DslParameterInfo>>();

        private static readonly IReadOnlyDictionary<string, DslParameterInfo> s_bodyParameters = new Dictionary<string, DslParameterInfo>
        {
            { "Body", new DslParameterInfo("Body", "scriptblock") },
        };

        protected override IReadOnlyDictionary<string, DslParameterInfo> VisitArray(ArrayType armArray)
        {
            return Visit(armArray.ItemType.Type);
        }

        protected override IReadOnlyDictionary<string, DslParameterInfo> VisitBuiltin(BuiltInType armBuiltin)
        {
            return s_builtinParameters.GetOrAdd(armBuiltin.Kind, kind =>
                new Dictionary<string, DslParameterInfo> { { "Value", new DslParameterInfo("Value", kind.AsPowerShellTypeString()) } });
        }

        protected override IReadOnlyDictionary<string, DslParameterInfo> VisitDiscriminatedObject(DiscriminatedObjectType armDiscriminatedObject)
        {
            return s_bodyParameters;
        }

        protected override IReadOnlyDictionary<string, DslParameterInfo> VisitObject(ObjectType armObject)
        {
            return s_bodyParameters;
        }

        protected override IReadOnlyDictionary<string, DslParameterInfo> VisitResource(ResourceType armResource)
        {
            throw new ArgumentException($"Cannot generate parameters for an ARM resource keyword");
        }

        protected override IReadOnlyDictionary<string, DslParameterInfo> VisitString(StringLiteralType armString)
        {
            return new Dictionary<string, DslParameterInfo>
            {
                { "Value", new DslParameterInfo("Value", "string", new [] { armString.Value })  },
            };
        }

        protected override IReadOnlyDictionary<string, DslParameterInfo> VisitUnion(UnionType armUnion)
        {
            var values = new List<string>();
            foreach (ITypeReference type in armUnion.Elements)
            {
                if (type.Type is not StringLiteralType stringLiteral)
                {
                    throw new ArgumentException($"ARM union type has non-string-literal element of type '{type.Type}'");
                }

                values.Add(stringLiteral.Value);
            }

            return new Dictionary<string, DslParameterInfo>
            {
                { "Value", new DslParameterInfo("Value", "string", values) },
            };
        }
    }
}

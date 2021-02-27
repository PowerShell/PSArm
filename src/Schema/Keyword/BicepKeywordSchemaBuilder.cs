
// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using PSArm.Internal;
using System;
using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class BicepKeywordSchemaBuilder : BicepArmTypeVisitor<DslKeywordSchema>
    {
        private static readonly BicepKeywordParameterBuilder s_parameterBuilder = new BicepKeywordParameterBuilder();

        private static BicepKeywordSchemaBuilder Value { get; } = new BicepKeywordSchemaBuilder();

        public static DslKeywordSchema GetKeywordSchemaForBicepType(TypeBase bicepType)
            => Value.Visit(bicepType);

        private BicepKeywordSchemaBuilder()
        {
        }

        protected override DslKeywordSchema VisitArray(ArrayType armArray)
        {
            return Visit(armArray.ItemType.Type);
        }

        protected override DslKeywordSchema VisitBuiltin(BuiltInType armBuiltin)
        {
            return new OpenKeywordSchema(s_parameterBuilder.Visit(armBuiltin), useParametersForCompletions: false);
        }

        protected override DslKeywordSchema VisitDiscriminatedObject(DiscriminatedObjectType armDiscriminatedObject)
        {
            return new BicepDiscriminatedObjectKeywordSchema(armDiscriminatedObject);
        }

        protected override DslKeywordSchema VisitObject(ObjectType armObject)
        {
            return new BicepObjectKeywordSchema(armObject);
        }

        protected override DslKeywordSchema VisitResource(ResourceType armResource)
        {
            throw new ArgumentException($"Cannot generate schema for ARM Resource type");
        }

        protected override DslKeywordSchema VisitString(StringLiteralType armString)
        {
            return new OpenKeywordSchema(s_parameterBuilder.Visit(armString), useParametersForCompletions: true);
        }

        protected override DslKeywordSchema VisitUnion(UnionType armUnion)
        {
            return new OpenKeywordSchema(s_parameterBuilder.Visit(armUnion), useParametersForCompletions: true);
        }
    }
}


// Copyright (c) Microsoft Corporation.

using Azure.Bicep.Types.Concrete;
using System;

namespace PSArm.Internal
{
    internal abstract class BicepArmTypeVisitor<TResult>
    {
        public TResult Visit(TypeBase armType)
        {
            switch (armType)
            {
                case ArrayType armArray:
                    return VisitArray(armArray);

                case BuiltInType armBuiltin:
                    return VisitBuiltin(armBuiltin);

                case DiscriminatedObjectType armDiscriminatedObject:
                    return VisitDiscriminatedObject(armDiscriminatedObject);

                case ObjectType armObject:
                    return VisitObject(armObject);

                case ResourceType armResource:
                    return VisitResource(armResource);

                case StringLiteralType armString:
                    return VisitString(armString);

                case UnionType armUnion:
                    return VisitUnion(armUnion);

                default:
                    throw new ArgumentException($"Unknown ARM schema type: '{armType}'");
            }
        }

        protected abstract TResult VisitArray(ArrayType armArray);

        protected abstract TResult VisitBuiltin(BuiltInType armBuiltin);

        protected abstract TResult VisitDiscriminatedObject(DiscriminatedObjectType armDiscriminatedObject);

        protected abstract TResult VisitObject(ObjectType armObject);

        protected abstract TResult VisitResource(ResourceType armResource);

        protected abstract TResult VisitString(StringLiteralType armString);

        protected abstract TResult VisitUnion(UnionType armUnion);
    }
}

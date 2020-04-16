using RobImpl.ArmSchema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace RobImpl
{
    /// <summary>
    /// ⊤: Schema matching any input; always satisfied.
    /// </summary>
    public class Top : ArmJsonSchema
    {
        public static Top Value { get; } = new Top();

        private Top()
        {
        }

        public override object Clone()
        {
            return Top.Value;
        }
    }

    /// <summary>
    /// ⊥: Schema matching no input; unsatisfiable.
    /// </summary>
    public class Bottom : ArmJsonSchema
    {

        public static Bottom Value { get; } = new Bottom();

        private Bottom()
        {
        }

        public override object Clone()
        {
            return new Bottom();
        }
    }

    public static class SchemaFolding
    {
        public static ArmJsonSchema Fold(this ArmJsonSchema schema)
        {
            switch (schema)
            {
                case ArmAllOfCombinator allOf:
                    return Intersect(allOf.AllOf.FoldAll());

                case ArmAnyOfCombinator anyOf:
                    return MergeAnyOf(anyOf.AnyOf.FoldAll());

                case ArmOneOfCombinator oneOf:
                    return MergeOneOf(oneOf.OneOf.FoldAll());

                case ArmNotCombinator not:
                    return new ArmNotCombinator { Not = not.Not.Fold() };

                case ArmListSchema list:
                    return new ArmListSchema
                    {
                        Items = list.Items.Fold(),
                        Length = list.Length,
                        UniqueItems = list.UniqueItems,
                    }.SetCommonFields(list);

                case ArmObjectSchema obj:
                    return new ArmObjectSchema
                    {
                        AdditionalProperties = obj.AdditionalProperties.Fold(),
                        MaxProperties = obj.MaxProperties,
                        MinProperties = obj.MinProperties,
                        Required = obj.Required,
                        Properties = obj.Properties.FoldAll(),
                    }.SetCommonFields(obj);

                default:
                    return schema;
            }
        }

        private static ArmJsonSchema Intersect(IReadOnlyList<ArmJsonSchema> schemas)
        {
            if (schemas == null || schemas.Count == 0)
            {
                return Top.Value;
            }

            // Child allOfs should have been dealt with by this stage,
            // meaning we expect only other schema types.
            // Really we expect one concrete schema type, plus some any/one-ofs.
            // nots are currently too hard to deal with

            var concreteSchemas = new List<ArmConcreteSchema>();
            var anyOfs = new List<ArmAnyOfCombinator>();
            var oneOfs = new List<ArmOneOfCombinator>();
            foreach (ArmJsonSchema schema in schemas)
            {
                switch (schema)
                {
                    case Bottom _:
                        // We cannot unify
                        Console.WriteLine("Encountered bottom in intersection");
                        return Bottom.Value;

                    case Top _:
                        // Accepts anything, so ignore
                        continue;

                    case ArmOneOfCombinator oneOf:
                        oneOfs.Add(oneOf);
                        continue;

                    case ArmAnyOfCombinator anyOf:
                        anyOfs.Add(anyOf);
                        continue;

                            case ArmNotCombinator _:
                        Console.WriteLine("not schema encountered, skipping");
                        continue;

                    case ArmConcreteSchema concreteSchema:
                        concreteSchemas.Add(concreteSchema);
                        continue;
                }
            }

            if (anyOfs.Count > 0 && oneOfs.Count > 0)
            {
                Console.WriteLine("Cannot combine anyofs and oneofs, returning bottom");
                return Bottom.Value;
            }

            ArmJsonSchema combinedConcreteSchema = IntersectConcrete(concreteSchemas);

            if (combinedConcreteSchema is Bottom bottom)
            {
                Console.WriteLine("Unable to unify concrete schemas, returning bottom");
                return bottom;
            }

            if (anyOfs.Count > 0)
            {
                var newChildSchemas = new List<ArmJsonSchema>();
                foreach (ArmAnyOfCombinator anyOf in anyOfs)
                {
                    foreach (ArmJsonSchema childSchema in anyOf.AnyOf)
                    {
                        newChildSchemas.Add(IntersectConcrete((ArmConcreteSchema)combinedConcreteSchema, (ArmConcreteSchema)childSchema));
                    }
                }

                return new ArmAnyOfCombinator
                {
                    AnyOf = newChildSchemas.ToArray(),
                };
            }

            if (oneOfs.Count > 0)
            {
                var newChildSchemas = new List<ArmJsonSchema>();
                foreach (ArmOneOfCombinator oneOf in oneOfs)
                {
                    foreach (ArmJsonSchema childSchema in oneOf.OneOf)
                    {
                        newChildSchemas.Add(IntersectConcrete((ArmConcreteSchema)combinedConcreteSchema, (ArmConcreteSchema)childSchema));
                    }
                }

                return new ArmOneOfCombinator
                {
                    OneOf = newChildSchemas.ToArray(),
                };
            }

            // We have no oneOfs or anyOfs, so it's just the concrete schema
            return combinedConcreteSchema;
        }

        private static ArmJsonSchema IntersectConcrete(IReadOnlyList<ArmConcreteSchema> schemas)
        {
            if (schemas == null || schemas.Count == 0)
            {
                return Top.Value;
            }

            if (schemas.Count == 1)
            {
                return schemas[0];
            }

            var product = (ArmConcreteSchema)schemas[0].Clone();
            for (int i = 1; i < schemas.Count; i++)
            {
                ArmJsonSchema result = IntersectConcrete(product, schemas[i]);

                if (result is Bottom bottom)
                {
                    return bottom;
                }

                product = (ArmConcreteSchema)result;
            }

            return product;
        }

        private static ArmJsonSchema IntersectConcrete(ArmConcreteSchema left, ArmConcreteSchema right)
        {
            if (right.Type == null || right.Type.Length == 0)
            {
            }

            if (left.Type == null || left.Type.Length == 0)
            {
            }

            if (left.Type.Length == 1
                && right.Type.Length == 1)
            {
                if (left.Type[0] != right.Type[0])
                {
                    return Bottom.Value;
                }

                switch (left)
                {
                    case ArmObjectSchema lObj:
                        if (!(right is ArmObjectSchema rObj))
                        {
                            return Bottom.Value;
                        }

                        return lObj;

                    case ArmStringSchema lStr:
                        if (!(right is ArmStringSchema rStr))
                        {
                            return Bottom.Value;
                        }

                        // TODO: Actually merge these, for now not an issue
                        return lStr;

                    case ArmListSchema lList:
                        if (!(right is ArmListSchema rList))
                        {
                            return Bottom.Value;
                        }

                        // TODO: Actually merge these, for now not an issue
                        return lList;

                    case ArmTupleSchema lTuple:
                        if (!(right is ArmTupleSchema rTuple))
                        {
                            return Bottom.Value;
                        }

                        // TODO: Actually merge these, for now not an issue
                        return lTuple;

                    case ArmIntegerSchema lInt:
                        if (!(right is ArmIntegerSchema rInt))
                        {
                            return Bottom.Value;
                        }

                        // TODO: Actually merge these, for now not an issue
                        return lInt;

                    case ArmNumberSchema lNum:
                        if (!(right is ArmNumberSchema rNum))
                        {
                            return Bottom.Value;
                        }

                        // TODO: Actually merge these, for now not an issue
                        return lNum;


                    case ArmBooleanSchema _:
                        if (!(right is ArmBooleanSchema))
                        {
                            return Bottom.Value;
                        }
                        return left;


                    case ArmNullSchema _:
                        if (!(right is ArmNullSchema))
                        {
                            return Bottom.Value;
                        }
                        return left;
                }
            }
        }

        private static ArmConcreteSchema MergeCommonFields(ArmConcreteSchema left, ArmConcreteSchema right)
        {
            if (left.Enum == null || right.Enum == null)
            {
                left.Enum = null;
            }
            else
            {
                var enums = new List<object>();
                foreach (object lItem in left.Enum)
                {
                    foreach (object rItem in right.Enum)
                    {
                        if (object.Equals(lItem, rItem))
                        {
                            enums.Add(lItem);
                        }
                    }
                }
                left.Enum = enums.ToArray();
            }

            return left;
        }

        private static ArmAnyOfCombinator MergeAnyOf(ArmJsonSchema[] schemas)
        {
            var anyOfList = new List<ArmJsonSchema>(schemas.Length);

            for (int i = 0; i < schemas.Length; i++)
            {
                ArmJsonSchema child = schemas[i];
                switch (child)
                {
                    case ArmAnyOfCombinator childAnyOf:
                        anyOfList.AddRange(childAnyOf.AnyOf);
                        continue;

                    default:
                        anyOfList.Add(child);
                        continue;
                }
            }

            return new ArmAnyOfCombinator
            {
                AnyOf = anyOfList.ToArray(),
            };
        }

        private static ArmOneOfCombinator MergeOneOf(ArmJsonSchema[] schemas)
        {
            var oneOfList = new List<ArmJsonSchema>(schemas.Length);

            for (int i = 0; i < schemas.Length; i++)
            {
                ArmJsonSchema child = schemas[i];
                switch (child)
                {
                    case ArmOneOfCombinator childOneOf:
                        oneOfList.AddRange(childOneOf.OneOf);
                        continue;

                    default:
                        oneOfList.Add(child);
                        continue;
                }
            }

            return new ArmOneOfCombinator
            {
                OneOf = oneOfList.ToArray(),
            };
        }

        private static ArmJsonSchema[] FoldAll(this ArmJsonSchema[] schemas)
        {
            var result = new ArmJsonSchema[schemas.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = schemas[i].Fold();
            }
            return result;
        }

        private static Dictionary<string, ArmJsonSchema> FoldAll(this Dictionary<string, ArmJsonSchema> propertySchemas)
        {
            var dict = new Dictionary<string, ArmJsonSchema>(propertySchemas.Count);
            foreach (KeyValuePair<string, ArmJsonSchema> propertySchema in propertySchemas)
            {
                dict[propertySchema.Key] = propertySchema.Value.Fold();
            }
            return dict;
        }

        private static Union<bool, ArmJsonSchema> Fold(this Union<bool, ArmJsonSchema> union)
        {
            return union.Match(
                _ => union,
                additionalSchema => new Union<bool, ArmJsonSchema>.Case2(additionalSchema.Fold()));
        }

        private static ArmConcreteSchema SetCommonFields(this ArmConcreteSchema copy, ArmConcreteSchema original)
        {
            copy.Default = original.Default;
            copy.Description = original.Description;
            copy.Enum = original.Enum;
            copy.Id = original.Id;
            copy.SchemaVersion = original.SchemaVersion;
            copy.Title = original.Title;
            return copy;
        }
    }
}

using RobImpl.ArmSchema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
            return Bottom.Value;
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
                    return MergeAnyOf(anyOf.AnyOf.FoldAll(removeExpressions: true));

                case ArmOneOfCombinator oneOf:
                    return MergeOneOf(oneOf.OneOf.FoldAll(removeExpressions: true));

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

            if (anyOfs.Count > 1
                || oneOfs.Count > 1
                || anyOfs.Count > 0 && oneOfs.Count > 0)
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
                var childSchemas = new List<ArmJsonSchema>();
                foreach (ArmJsonSchema currentChild in anyOfs[0].AnyOf)
                {
                    childSchemas.Add(IntersectConcrete((ArmConcreteSchema)combinedConcreteSchema, (ArmConcreteSchema)currentChild));
                }

                return new ArmAnyOfCombinator
                {
                    AnyOf = childSchemas.ToArray(),
                };
            }

            if (oneOfs.Count > 0)
            {
                var childSchemas = new List<ArmJsonSchema>();
                foreach (ArmJsonSchema currentChild in oneOfs[0].OneOf)
                {
                    childSchemas.Add(IntersectConcrete((ArmConcreteSchema)combinedConcreteSchema, (ArmConcreteSchema)currentChild));
                }

                return new ArmOneOfCombinator
                {
                    OneOf = childSchemas.ToArray(),
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
                return IntersectCommonFields(left, right);
            }

            if (left.Type == null || left.Type.Length == 0)
            {
                return IntersectCommonFields((ArmConcreteSchema)right.Clone(), left);
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

                        return IntersectObjects(lObj, rObj);

                    case ArmStringSchema lStr:
                        if (!(right is ArmStringSchema rStr))
                        {
                            return Bottom.Value;
                        }

                        return IntersectStrings(lStr, rStr);

                    case ArmListSchema lList:
                        if (!(right is ArmListSchema rList))
                        {
                            return Bottom.Value;
                        }

                        return IntersectLists(lList, rList);

                    case ArmTupleSchema lTuple:
                        if (!(right is ArmTupleSchema rTuple))
                        {
                            return Bottom.Value;
                        }

                        return IntersectTuples(lTuple, rTuple);

                    case ArmIntegerSchema lInt:
                        if (!(right is ArmIntegerSchema rInt))
                        {
                            return Bottom.Value;
                        }

                        return IntersectIntegers(lInt, rInt);

                    case ArmNumberSchema lNum:
                        if (!(right is ArmNumberSchema rNum))
                        {
                            return Bottom.Value;
                        }

                        return IntersectNumbers(lNum, rNum);


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

            return IntersectMultitype(left, right);
        }

        private static ArmJsonSchema IntersectObjects(ArmObjectSchema left, ArmObjectSchema right)
        {
            foreach (KeyValuePair<string, ArmJsonSchema> property in right.Properties)
            {
                if (left.Properties.TryGetValue(property.Key, out ArmJsonSchema lProperty))
                {
                    left.Properties[property.Key] = Intersect(new[] { property.Value, lProperty });
                }
                else
                {
                    left.Properties[property.Key] = (ArmJsonSchema)property.Value.Clone();
                }
            }

            if (right.Required != null)
            {
                List<string> requiredProperties = null;
                foreach (string rRequiredProperty in right.Required)
                {
                    if (left.Required != null && !left.Required.Contains(rRequiredProperty))
                    {
                        if (requiredProperties == null)
                        {
                            requiredProperties = new List<string>(left.Required);
                        }

                        requiredProperties.Add(rRequiredProperty);
                    }
                }

                if (requiredProperties != null)
                {
                    left.Required = requiredProperties.ToArray();
                }
            }

            left.MaxProperties = GetLesser(left.MaxProperties, right.MaxProperties);
            left.MinProperties = GetLesser(left.MinProperties, right.MinProperties);

            if (!TryIntersectUnions(
                    left.AdditionalProperties,
                    right.AdditionalProperties,
                    boolean => boolean,
                    schema => (ArmJsonSchema)schema.Clone(),
                    (lBool, rBool) => lBool && rBool,
                    (lSchema, rSchema) => Intersect(new[] { lSchema, rSchema }),
                    out Union<bool, ArmJsonSchema> intersectedAdditionalProperties))
            {
                return Bottom.Value;
            }
            left.AdditionalProperties = intersectedAdditionalProperties;

            return left;
        }

        private static bool TryIntersectUnions<T1, T2>(
            Union<T1, T2> left,
            Union<T1, T2> right,
            Func<T1, T1> clone1,
            Func<T2, T2> clone2,
            Func<T1, T1, T1> intersect1,
            Func<T2, T2, T2> intersect2,
            out Union<T1, T2> result)
        {
            if (right == null)
            {
                result = left;
                return true;
            }

            if (left == null)
            {
                result = right.Match<Union<T1, T2>>(
                    case1 => new Union<T1, T2>.Case1(clone1(case1)),
                    case2 => new Union<T1, T2>.Case2(clone2(case2)));

                return true;
            }

            bool succeded = true;
            result = left.Match(
                lCase1 =>
                    right.Match<Union<T1, T2>>(
                        rCase1 => new Union<T1, T2>.Case1(intersect1(lCase1, rCase1)),
                        rCase2 =>
                        {
                            succeded = false;
                            return null;
                        }),
                lCase2 =>
                    right.Match<Union<T1, T2>>(
                        rCase1 =>
                        {
                            succeded = false;
                            return null;
                        },
                        rCase2 => new Union<T1, T2>.Case2(intersect2(lCase2, rCase2))));
            return succeded;
        }

        private static T GetLesser<T>(T left, T right)
        {
            if (right == null)
            {
                return left;
            }

            if (left == null || Comparer<T>.Default.Compare(right, left) < 0)
            {
                return right;
            }

            return left;
        }

        private static bool IsArmExpressionSchema(ArmJsonSchema schema)
        {
            if (!(schema is ArmStringSchema str))
            {
                return false;
            }

            return str.Description != null && str.Description.StartsWith("Deployment template expression");
        }

        private static ArmStringSchema IntersectStrings(ArmStringSchema left, ArmStringSchema right)
        {
            // TODO: Actually intersect the fields
            return IntersectCommonFields(left, right);
        }

        private static ArmListSchema IntersectLists(ArmListSchema left, ArmListSchema right)
        {
            // TODO: Actually intersect the fields
            return IntersectCommonFields(left, right);
        }

        private static ArmTupleSchema IntersectTuples(ArmTupleSchema left, ArmTupleSchema right)
        {
            // TODO: Actually intersect the fields
            return IntersectCommonFields(left, right);
        }

        private static ArmIntegerSchema IntersectIntegers(ArmIntegerSchema left, ArmIntegerSchema right)
        {
            // TODO: Actually intersect the fields
            return IntersectCommonFields(left, right);
        }

        private static ArmNumberSchema IntersectNumbers(ArmNumberSchema left, ArmNumberSchema right)
        {
            // TODO: Actually intersect the fields
            return IntersectCommonFields(left, right);
        }

        private static ArmConcreteSchema IntersectMultitype(ArmConcreteSchema left, ArmConcreteSchema right)
        {
            return IntersectCommonFields(left, right);
        }

        private static TSchema IntersectCommonFields<TSchema>(TSchema left, TSchema right) where TSchema : ArmConcreteSchema
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
                    if (right.Enum.Contains(lItem))
                    {
                        enums.Add(lItem);
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

                if (IsArmExpressionSchema(child))
                {
                    continue;
                }

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

                if (IsArmExpressionSchema(child))
                {
                    continue;
                }

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

        private static ArmJsonSchema[] FoldAll(this ArmJsonSchema[] schemas, bool removeExpressions = false)
        {
            var result = new List<ArmJsonSchema>();
            foreach (ArmJsonSchema schema in schemas)
            {
                if (removeExpressions && IsArmExpressionSchema(schema))
                {
                    continue;
                }

                result.Add(schema.Fold());
            }
            return result.ToArray(); ;
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

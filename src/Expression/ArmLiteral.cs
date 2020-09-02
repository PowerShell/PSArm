
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using System.ComponentModel;

namespace PSArm.Expression
{
    /// <summary>
    /// A concrete ARM value expression; a literal.
    /// </summary>
    [TypeConverter(typeof(ArmTypeConverter))]
    public abstract class ArmLiteral : IArmExpression
    {
        /// <summary>
        /// Instantiate the ARM literal with ARM parameters.
        /// Since an ARM literal can have no parameters, always returns itself.
        /// </summary>
        /// <param name="parameters">The parameters to instantiate with.</param>
        /// <returns>Itself.</returns>
        public IArmExpression Instantiate(IReadOnlyDictionary<string, IArmExpression> _) => this;

        /// <summary>
        /// Render the ARM literal as an ARM expression string.
        /// </summary>
        /// <returns>An ARM expression string expressing the literal.</returns>
        public abstract string ToExpressionString();

        /// <summary>
        /// Render the ARM literal as the inner ARM expression string, the expression string without the brackets,
        /// allowing composition.
        /// </summary>
        /// <returns>The inner part of the ARM expression string expressing the literal.</returns>
        public abstract string ToInnerExpressionString();

        /// <summary>
        /// Render the ARM literal as a string, showing it in expression string form.
        /// </summary>
        /// <returns>The ARM literal as an ARM expression string.</returns>
        public override string ToString() => ToExpressionString();

        /// <summary>
        /// Get the underlying literal value.
        /// </summary>
        /// <returns>The underlying .NET value of the literal.</returns>
        public abstract object GetValue();
    }

    /// <summary>
    /// A concrete ARM value of a given type.
    /// </summary>
    /// <typeparam name="T">The type of the ARM literal.</typeparam>
    [TypeConverter(typeof(ArmTypeConverter))]
    public abstract class ArmLiteral<T> : ArmLiteral
    {
        /// <summary>
        /// Create a literal expression object around the given value.
        /// </summary>
        /// <param name="value">The .NET value of the literal.</param>
        public ArmLiteral(T value)
        {
            Value = value;
        }

        /// <summary>
        /// The .NET value of the literal.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Helper method to get the literal value when the generic type is not known.
        /// </summary>
        /// <returns>The underlying literal value.</returns>
        public override object GetValue() => Value;
    }

    /// <summary>
    /// An ARM string literal.
    /// </summary>
    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmStringLiteral : ArmLiteral<string>
    {
        /// <summary>
        /// Create a new ARM string literal expression.
        /// </summary>
        /// <param name="value">The underlying string in the literal.</param>
        public ArmStringLiteral(string value) : base(value)
        {
        }

        /// <summary>
        /// Render the string literal as an ARM string.
        /// </summary>
        /// <returns>The string literal in ARM JSON string form.</returns>
        public override string ToExpressionString()
        {
            string val = Value.StartsWith("[") && Value.EndsWith("]")
                ? "[" + Value
                : Value;

            return val.Replace("\"", "\\\"");
        }

        /// <summary>
        /// Render the string literal to be composed as part of the larger ARM expression.
        /// </summary>
        /// <returns>The single quoted string to be inserted into a larger ARM expression.</returns>
        public override string ToInnerExpressionString()
        {
            return "'" + Value + "'";
        }
    }

    /// <summary>
    /// An ARM integer literal.
    /// </summary>
    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmIntLiteral : ArmLiteral<int>
    {
        /// <summary>
        /// Create a new ARM integer literal.
        /// </summary>
        /// <param name="value">The underlying integer value of the literal.</param>
        public ArmIntLiteral(int value) : base(value)
        {
        }

        /// <summary>
        /// Render the int literal as a JSON expression string.
        /// </summary>
        /// <returns></returns>
        public override string ToExpressionString() => Value.ToString();

        /// <summary>
        /// Render the int literal as a string for composition in larger ARM expressions.
        /// </summary>
        /// <returns></returns>
        public override string ToInnerExpressionString() => ToExpressionString();
    }

    /// <summary>
    /// An ARM boolean literal.
    /// </summary>
    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmBoolLiteral : ArmLiteral<bool>
    {
        /// <summary>
        /// Create a new ARM boolean literal around the given value.
        /// </summary>
        /// <param name="value">The underlying boolean value.</param>
        public ArmBoolLiteral(bool value) : base(value)
        {
        }

        /// <summary>
        /// Render the boolean as a JSON value.
        /// </summary>
        /// <returns></returns>
        public override string ToExpressionString()
        {
            return Value
                ? "true"
                : "false";
        }

        /// <summary>
        /// Render the boolean as an ARM expression string for expression composition.
        /// </summary>
        /// <returns></returns>
        public override string ToInnerExpressionString() => ToExpressionString();
    }

}
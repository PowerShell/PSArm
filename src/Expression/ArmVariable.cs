
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using PSArm.ArmBuilding;

namespace PSArm.Expression
{
    /// <summary>
    /// An ARM variable expression.
    /// Represents both the variable declaration and the ARM expression.
    /// </summary>
    public class ArmVariable : ArmOperation, IArmElement
    {
        public ArmVariable(string name, IArmExpression value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// The name of the ARM variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The given value of the variable.
        /// </summary>
        public IArmExpression Value { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            return new ArmVariable(Name, Value.Instantiate(parameters));
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append("variables('")
                .Append(Name)
                .Append("')")
                .ToString();
        }

        public JToken ToJson()
        {
            return new JValue(Value.ToExpressionString());
        }
    }

}
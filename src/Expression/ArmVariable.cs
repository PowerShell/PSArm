
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
        public ArmVariable(string name, IArmValue value)
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
        public IArmValue Value { get; }

        public override IArmValue Instantiate(IReadOnlyDictionary<string, IArmValue> parameters)
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

        public override JToken ToJson()
        {
            return Value.ToJson();
        }
    }

}
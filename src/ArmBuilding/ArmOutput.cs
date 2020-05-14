using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// Represents an item in the array of "outputs" in an ARM template.
    /// </summary>
    public class ArmOutput : IArmElement
    {
        /// <summary>
        /// The name of the output.
        /// </summary>
        public IArmExpression Name { get; set; }

        /// <summary>
        /// The ARM object type that the output emits.
        /// </summary>
        public IArmExpression Type { get; set; }

        /// <summary>
        /// The value that the output emits.
        /// </summary>
        public IArmExpression Value { get; set; }

        /// <summary>
        /// Render the output as ARM template JSON.
        /// </summary>
        /// <returns></returns>
        public JToken ToJson()
        {
            return new JObject
            {
                ["type"] = Type.ToExpressionString(),
                ["value"] = Value.ToExpressionString(),
            };
        }

        /// <summary>
        /// Instantiate any ARM parameters in this part of the template.
        /// </summary>
        /// <param name="parameters">Given ARM parameter values.</param>
        /// <returns>The instantiated output template.</returns>
        public ArmOutput Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmOutput
            {
                Name = Name.Instantiate(parameters),
                Type = Type.Instantiate(parameters),
                Value = Value.Instantiate(parameters),
            };
        }
    }
}

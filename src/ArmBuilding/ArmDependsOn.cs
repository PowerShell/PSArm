using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// Captures an ARM resource "dependsOn" field.
    /// </summary>
    public class ArmDependsOn : IArmElement
    {
        /// <summary>
        /// Construct a new DependsOn instance with the given value.
        /// </summary>
        /// <param name="value">The resource ID being depended on.</param>
        public ArmDependsOn(IArmExpression value)
        {
            Value = value;
        }

        /// <summary>
        /// The resource ID the parent resource depends on.
        /// </summary>
        public IArmExpression Value { get; }

        /// <summary>
        /// Instantiate this ARM template with any parameters provided.
        /// </summary>
        /// <param name="parameters">Parameters provided for template instantiation.</param>
        /// <returns>A new DependsOn element instantiated with the given parameters.</returns>
        public ArmDependsOn Instantiate(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            return new ArmDependsOn(Value.Instantiate(parameters));
        }

        /// <summary>
        /// Render the DependsOn element as ARM template JSON.
        /// </summary>
        /// <returns>A JSON object representation of the ARM template element.</returns>
        public JToken ToJson()
        {
            return new JValue(Value.ToExpressionString());
        }
    }
}

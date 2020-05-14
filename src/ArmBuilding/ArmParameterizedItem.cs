using System.Collections.Generic;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// Represents an ARM element with a parameter field.
    /// </summary>
    public abstract class ArmParameterizedItem : ArmPropertyInstance
    {
        /// <summary>
        /// Create a new parameterized item with a given property name.
        /// </summary>
        /// <param name="propertyName">The property name this element will live under in its parent.</param>
        protected ArmParameterizedItem(string propertyName)
            : base(propertyName)
        {
            Parameters = new Dictionary<string, IArmExpression>();
        }

        /// <summary>
        /// The parameters of this iteam.
        /// </summary>
        public Dictionary<string, IArmExpression> Parameters { get; protected set; }

        /// <summary>
        /// Instantiate all the parameter field values on this item.
        /// </summary>
        /// <param name="parameters">The ARM parameter values to instantiate the parameters field values with.</param>
        /// <returns>A fully instantiated set of parameters.</returns>
        protected Dictionary<string, IArmExpression> InstantiateParameters(IReadOnlyDictionary<string, IArmExpression> parameters)
        {
            if (Parameters == null)
            {
                return null;
            }

            var dict = new Dictionary<string, IArmExpression>();
            foreach (KeyValuePair<string, IArmExpression> parameter in Parameters)
            {
                dict[parameter.Key] = parameter.Value.Instantiate(parameters);
            }
            return dict;
        }
    }
}
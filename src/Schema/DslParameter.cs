using Newtonsoft.Json;
using System.Collections.Generic;

namespace PSArm.Schema
{
    /// <summary>
    /// A parameter on an ARM element.
    /// </summary>
    public class DslParameter
    {
        /// <summary>
        /// Create a new ARM parameter.
        /// </summary>
        public DslParameter()
        {
            Enum = new List<object>();
        }

        /// <summary>
        /// The name of the parameter.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// List of values the parameter may take. May be null.
        /// </summary>
        [JsonProperty("enum")]
        public List<object> Enum { get; set; }
    }

}

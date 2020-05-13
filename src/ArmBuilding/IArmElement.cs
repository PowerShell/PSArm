using Newtonsoft.Json.Linq;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// A JSON value element in an ARM template.
    /// </summary>
    public interface IArmElement
    {
        /// <summary>
        /// Render the ARM element to JSON.
        /// </summary>
        /// <returns>A JSON object representing the ARM template JSON form of this element.</returns>
        JToken ToJson();
    }
}
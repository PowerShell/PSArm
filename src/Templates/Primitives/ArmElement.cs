using Newtonsoft.Json.Linq;
using System.Management.Automation;

namespace PSArm.Templates.Primitives
{
    public abstract class ArmElement
    {
        public abstract JToken ToJson();

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}

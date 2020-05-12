using Newtonsoft.Json.Linq;

namespace PSArm.ArmBuilding
{
    public interface IArmElement
    {
        JToken ToJson();
    }
}
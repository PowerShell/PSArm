using Newtonsoft.Json.Linq;
using PSArm.Serialization;
using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public abstract class ArmElement
    {
        public JToken ToJson()
        {
            return Visit(new ArmJsonBuildingVisitor());
        }

        public abstract TResult Visit<TResult>(IArmVisitor<TResult> visitor);

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}

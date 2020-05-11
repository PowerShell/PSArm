using System.Management.Automation;
using PSArm.ArmBuilding;

namespace PSArm.Commands.ArmBuilding
{
    [Alias("ArrayItem")]
    [Cmdlet(VerbsCommon.New, "ArmArrayItem")]
    public class NewArmArrayItemCommand : NewArmPropertyBlockCommand
    {
        protected override ArmPropertyObject CreatePropertyObject()
        {
            return new ArmPropertyArrayItem(Name);
        }
    }
}

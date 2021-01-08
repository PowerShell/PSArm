using PSArm.Commands.Internal;
using PSArm.Templates.Primitives;
using PSArm.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSArm.Commands.Primitive
{
    [Alias(Name)]
    [Cmdlet(VerbsCommon.New, ModuleConstants.ModulePrefix + "Array", DefaultParameterSetName = "Body")]
    public class NewPSArmArrayCommand : PSArmKeywordCommand
    {
        public const string Name = "ArmArray";

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Body")]
        public ScriptBlock Body { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ObjectArray")]
        public object[] Values { get; set; }

        protected override void EndProcessing()
        {
            if (Body != null)
            {
                WriteArmArrayElement(Body);
                return;
            }

            var armArray = new ArmArray();
            for (int i = 0; i < Values.Length; i++)
            {
                object currVal = Values[i];
                if (!ArmElementConversion.TryConvertToArmElement(currVal, out ArmElement element))
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new InvalidCastException($"Unable to convert value '{currVal}' of type '{currVal.GetType()}' to type '{typeof(ArmElement)}'"),
                            "InvalidArmTypeCase",
                            ErrorCategory.InvalidArgument,
                            currVal));
                    return;
                }
                armArray.Add(element);
            }
            WriteObject(armArray);
        }
    }
}

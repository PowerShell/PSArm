
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;

namespace PSArm.Commands.Expression
{
    [Alias("ResourceGroup")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinResourceGroup")]
    public class InvokeArmBuiltinResourceGroupCommand : ArmBuiltinCommand
    {
        public InvokeArmBuiltinResourceGroupCommand() : base("resourceGroup")
        {
        }
    }

}
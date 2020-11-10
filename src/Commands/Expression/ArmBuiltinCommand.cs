
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Management.Automation;
using PSArm.Expression;

namespace PSArm.Commands.Expression
{
    public abstract class ArmBuiltinCommand : ArmCallCommand
    {
        protected ArmBuiltinCommand(string function)
        {
            Function = function;
        }
    }
}
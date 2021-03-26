
// Copyright (c) Microsoft Corporation.

using System;
using System.Management.Automation;

namespace PSArm.Commands.Internal
{
    internal static class CmdletExtensions
    {
        public static void ThrowTerminatingError(
            this Cmdlet cmdlet,
            Exception e,
            string errorId,
            ErrorCategory errorCategory,
            object target = null)
        {
            cmdlet.ThrowTerminatingError(new ErrorRecord(e, errorId, errorCategory, target));
        }

    }
}

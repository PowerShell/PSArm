
// Copyright (c) Microsoft Corporation.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

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

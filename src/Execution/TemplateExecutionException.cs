
// Copyright (c) Microsoft Corporation.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Execution
{
    public class TemplateExecutionException : Exception
    {
        public TemplateExecutionException(string message, ErrorRecord errorRecord)
            : base(message)
        {
            ErrorRecord = errorRecord;
        }

        public TemplateExecutionException(ErrorRecord errorRecord)
        {
            ErrorRecord = errorRecord;
        }

        public ErrorRecord ErrorRecord { get; }
    }
}

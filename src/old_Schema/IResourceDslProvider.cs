using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace PSArm.Schema
{
    public interface IResourceDslProvider
    {
        IReadOnlyDictionary<string, ScriptBlock> GetResourceDsl(
            string resourceNamespace,
            string resourceType,
            string apiVersion);
    }
}

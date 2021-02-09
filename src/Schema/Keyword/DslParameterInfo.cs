using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema.Keyword
{
    internal class DslParameterInfo
    {
        public string Name { get; }

        public IReadOnlyList<string> Values { get; }

        public string Type { get; }
    }
}


// Copyright (c) Microsoft Corporation.

using System.Collections.Generic;

namespace PSArm.Schema.Keyword
{
    internal class DslParameterInfo
    {
        public DslParameterInfo(string name, string type, IReadOnlyList<string> values)
            : this(name, type)
        {
            Values = values;
        }

        public DslParameterInfo(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public string Type { get; }

        public IReadOnlyList<string> Values { get; }
    }
}

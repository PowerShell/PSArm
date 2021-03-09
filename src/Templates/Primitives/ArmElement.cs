
// Copyright (c) Microsoft Corporation.

using Newtonsoft.Json.Linq;
using PSArm.Serialization;
using PSArm.Templates.Visitors;
using PSArm.Types;
using System.Collections.Generic;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public abstract class ArmElement : IArmElement
    {
        public JToken ToJson()
        {
            return Visit(new ArmJsonBuildingVisitor());
        }

        public abstract TResult Visit<TResult>(IArmVisitor<TResult> visitor);

        public abstract IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters);

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}

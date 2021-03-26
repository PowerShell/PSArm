
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

        public TResult RunVisit<TResult>(IArmVisitor<TResult> visitor)
        {
            return Visit(visitor);
        }

        public VisitAction RunVisit(ArmTravsersingVisitor visitor)
        {
            VisitAction result = Visit(visitor);
            visitor.PostVisit(this);
            return result;
        }

        protected abstract TResult Visit<TResult>(IArmVisitor<TResult> visitor);

        public abstract IArmElement Instantiate(IReadOnlyDictionary<IArmString, ArmElement> parameters);

        public string ToJsonString()
        {
            return ToJson().ToString();
        }
    }
}

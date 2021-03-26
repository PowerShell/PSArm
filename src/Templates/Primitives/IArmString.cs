
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Templates.Visitors;
using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmStringConverter))]
    public interface IArmString : IArmExpression
    {
        string ToExpressionString();

        string ToIdentifierString();

        TResult RunVisit<TResult>(IArmVisitor<TResult> visitor);

        VisitAction RunVisit(ArmTravsersingVisitor visitor);
    }
}

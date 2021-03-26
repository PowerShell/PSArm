
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmExpressionConverter))]
    public interface IArmExpression : IArmElement
    {
        string ToInnerExpressionString();
    }

    [TypeConverter(typeof(ArmExpressionConverter))]
    public abstract class ArmExpression : ArmElement, IArmExpression
    {
        public abstract string ToInnerExpressionString();

        public override string ToString() => ToJsonString();
    }
}

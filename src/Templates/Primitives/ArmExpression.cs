
// Copyright (c) Microsoft Corporation.

using PSArm.Types;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmExpressionConverter))]
    public interface IArmExpression
    {
        string ToInnerExpressionString();
    }

    [TypeConverter(typeof(ArmExpressionConverter))]
    public abstract class ArmExpression : ArmElement, IArmExpression
    {
        public abstract string ToInnerExpressionString();
    }
}

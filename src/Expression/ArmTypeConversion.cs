using System;
using System.Management.Automation;

namespace PSArm.Expression
{
    internal static class ArmTypeConversion
    {
        public static IArmExpression Convert(object obj)
        {
            switch (obj)
            {
                case null:
                    return null;

                case IArmExpression expression:
                    return expression;

                case PSObject psObj:
                    return Convert(psObj.BaseObject);

                case string s:
                    return new ArmStringLiteral(s);

                case int i:
                    return new ArmIntLiteral(i);

                case bool b:
                    return new ArmBoolLiteral(b);

                default:
                    throw new ArgumentException($"Unable to covert value '{obj}' of type '{obj.GetType()}' to IArmExpression");
            }
        }
    }

}
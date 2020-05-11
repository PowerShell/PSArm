using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;
using PSArm.Expression;

namespace PSArm.Completion
{
    public static class ArmTypeAccelerators
    {
        internal const string ArmVariable = "ArmVariable";

        internal const string ArmParameter = "ArmParameter";

        private static Type s_psTypeAcceleratorsType = typeof(PSObject).Assembly
            .GetType("System.Management.Automation.TypeAccelerators");

        private static MethodInfo s_psTypeAcceleratorsAddMethod = s_psTypeAcceleratorsType.GetMethod("Add");

        private static MethodInfo s_psTypeAcceleratorsRemoveMethod = s_psTypeAcceleratorsType.GetMethod("Remove");

        private static IReadOnlyDictionary<string, Type> s_armTypeAccelerators = new Dictionary<string, Type>
        {
            { ArmVariable, typeof(ArmVariable) },
            { ArmParameter, typeof(ArmParameter<>) },
        };

        public static void Load()
        {
            var paramArray = new object[2];
            foreach (KeyValuePair<string, Type> armAccelerator in s_armTypeAccelerators)
            {
                paramArray[0] = armAccelerator.Key;
                paramArray[1] = armAccelerator.Value;
                s_psTypeAcceleratorsAddMethod.Invoke(obj: null, paramArray);
            }
        }

        public static void Unload()
        {
            var paramArray = new object[1];
            foreach (string accelerator in s_armTypeAccelerators.Keys)
            {
                paramArray[0] = accelerator;
                s_psTypeAcceleratorsRemoveMethod.Invoke(obj: null, paramArray);
            }
        }
    }
}
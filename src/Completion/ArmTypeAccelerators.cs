using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PSArm.Completion
{
    public static class ArmTypeAccelerators
    {
        private static IReadOnlyDictionary<string, Type> s_armTypeAccelerators = new Dictionary<string, Type>
        {
            { "ArmVariable", typeof(ArmVariable) }
        };

        private static Lazy<Dictionary<string, Type>> s_psTypeAcceleratorTable = new Lazy<Dictionary<string, Type>>(GetPSTypeAcceleratorsDict);

        public static void Load()
        {
            foreach (KeyValuePair<string, Type> entry in s_armTypeAccelerators)
            {
                s_psTypeAcceleratorTable.Value[entry.Key] = entry.Value;
            }
        }

        public static void Unload()
        {
            foreach (KeyValuePair<string, Type> entry in s_armTypeAccelerators)
            {
                s_psTypeAcceleratorTable.Value.Remove(entry.Key);
            }
        }

        private static Dictionary<string, Type> GetPSTypeAcceleratorsDict()
        {
            return (Dictionary<string, Type>)typeof(PSObject)
                .Assembly
                .GetType("System.Management.Automation.TypeAccelerators")
                .GetMethod("get_Get")
                .Invoke(obj: null, Array.Empty<object>());
        }
    }
}
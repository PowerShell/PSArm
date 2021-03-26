
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// All rights reserved.

using PSArm.Templates;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;

namespace PSArm.Types
{
    /// <summary>
    /// Lists and loads/unloads type accelerators for ARM.
    /// </summary>
    public class ArmTypeAccelerators : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        /// <summary>
        /// Type accelerator for an ARM variable.
        /// </summary>
        internal const string ArmVariable = "ArmVariable";

        /// <summary>
        /// Type accelerator for an ARM parameter.
        /// </summary>
        internal const string ArmParameter = "ArmParameter";

        internal const string SecureObject = "SecureObject";

        private static Type s_psTypeAcceleratorsType = typeof(PSObject).Assembly
            .GetType("System.Management.Automation.TypeAccelerators");

        private static MethodInfo s_psTypeAcceleratorsAddMethod = s_psTypeAcceleratorsType.GetMethod("Add");

        private static MethodInfo s_psTypeAcceleratorsRemoveMethod = s_psTypeAcceleratorsType.GetMethod("Remove");

        private static IReadOnlyDictionary<string, Type> s_armTypeAccelerators = new Dictionary<string, Type>
        {
            { ArmVariable, typeof(ArmVariable) },
            { ArmParameter, typeof(ArmParameter<>) },
            { SecureObject, typeof(SecureObject) },
        };

        /// <summary>
        /// Install the ARM type accelerators into PowerShell's type accelerator dictionary.
        /// </summary>
        public void OnImport()
        {
            var paramArray = new object[2];
            foreach (KeyValuePair<string, Type> armAccelerator in s_armTypeAccelerators)
            {
                paramArray[0] = armAccelerator.Key;
                paramArray[1] = armAccelerator.Value;
                s_psTypeAcceleratorsAddMethod.Invoke(obj: null, paramArray);
            }
        }

        /// <summary>
        /// Remove the ARM type accelerators from PowerShell's type accelerator dictionary.
        /// </summary>
        public void OnRemove(PSModuleInfo module)
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
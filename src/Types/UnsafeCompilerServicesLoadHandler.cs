
// Copyright (c) Microsoft Corporation.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PSArm.Types
{
#if !CoreCLR
    public class UnsafeCompilerServicesLoadHandler : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        private static readonly string s_moduleAsmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public void OnImport()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }

        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= HandleAssemblyResolve;
        }

        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var requiredAsmName = new AssemblyName(args.Name);

            string possibleAsmPath = Path.Combine(s_moduleAsmDir, $"{requiredAsmName.Name}.dll");

            AssemblyName bundledAsmName = null;
            try
            {
                bundledAsmName = AssemblyName.GetAssemblyName(possibleAsmPath);
            }
            catch
            {
                // If we don't bundle the assembly we're looking for, we don't have it so return nothing
                return null;
            }

            // Now make sure our version is greater
            if (bundledAsmName.Version < requiredAsmName.Version)
            {
                return null;
            }

            return Assembly.LoadFrom(possibleAsmPath);
        }
    }
#endif
}


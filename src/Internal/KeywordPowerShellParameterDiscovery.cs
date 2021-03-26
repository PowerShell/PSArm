
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Schema.Keyword;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;

namespace PSArm.Internal
{
    internal static class KeywordPowerShellParameterDiscovery
    {
        private static readonly IReadOnlyDictionary<Type, string> s_typeAccelerators = GetPSTypeAccelerators();

        public static IReadOnlyDictionary<string, DslParameterInfo> GetKeywordParametersFromCmdletType(Type type)
        {
            if (!typeof(Cmdlet).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type '{type}' must describe a PowerShell cmdlet.");
            }

            var parameters = new Dictionary<string, DslParameterInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var paramAttr = property.GetCustomAttribute<ParameterAttribute>();

                if (paramAttr is null)
                {
                    continue;
                }

                parameters[property.Name] = new DslParameterInfo(property.Name, GetPropertyType(property));
            }

            return parameters;
        }

        private static string GetPropertyType(PropertyInfo property)
        {
            if (s_typeAccelerators is not null
                && s_typeAccelerators.TryGetValue(property.PropertyType, out string typeAccelerator))
            {
                return typeAccelerator;
            }

            return property.PropertyType.ToString();
        }

        private static IReadOnlyDictionary<Type, string> GetPSTypeAccelerators()
        {
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators")?
                .GetMethod("get_Get")?
                .Invoke(obj: null, parameters: null) as Dictionary<string, Type>;

            if (typeAccelerators == null)
            {
                // Do our best
                return new Dictionary<Type, string>
                {
                    { typeof(string), "string" },
                    { typeof(ScriptBlock), "scriptblock" },
                    { typeof(object), "object" },
                    { typeof(int), "int" },
                    { typeof(double), "double" },
                    { typeof(Type), "type" },
                };
            }

            var typeLookupDict = new Dictionary<Type, string>(typeAccelerators.Count);
            foreach (KeyValuePair<string, Type> typeAccelerator in typeAccelerators)
            {
                typeLookupDict[typeAccelerator.Value] = typeAccelerator.Key;
            }
            return typeLookupDict;
        }
    }
}

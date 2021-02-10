using PSArm.Schema.Keyword;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace PSArm.Internal
{
    internal static class KeywordParameterDiscovery
    {
        public static IReadOnlyDictionary<string, DslParameterInfo> GetKeywordParametersFromCmdletType(Type type)
        {
            if (!typeof(Cmdlet).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type '{type}' must describe a PowerShell cmdlet.");
            }

            var parameters = new Dictionary<string, DslParameterInfo>();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var paramAttr = property.GetCustomAttribute<ParameterAttribute>();

                if (paramAttr is null)
                {
                    continue;
                }

                parameters[property.Name] = new DslParameterInfo(property.Name, property.PropertyType.ToString());
            }

            return parameters;
        }
    }
}

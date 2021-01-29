using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Schema
{
    public readonly struct ArmResourceName
    {
        public static ArmResourceName CreateFromFullyQualifiedName(string fullyQualifiedName)
        {
            int providerNameStartIndex = fullyQualifiedName.IndexOf('/') + 1;
            int apiVersionStartIndex = fullyQualifiedName.IndexOf('@', providerNameStartIndex) + 1;

            string providerNamespace = fullyQualifiedName.Substring(0, providerNameStartIndex - 1);
            string providerName = fullyQualifiedName.Substring(providerNameStartIndex, apiVersionStartIndex - providerNameStartIndex - 1);
            string providerApiVersion = fullyQualifiedName.Substring(apiVersionStartIndex);
            return new ArmResourceName(providerNamespace, providerName, providerApiVersion);
        }

        public static ArmResourceName CreateFromArmStrings(IArmString resourceNamespace, IArmString type, IArmString apiVersion)
        {
            return new ArmResourceName(
                (resourceNamespace as ArmStringLiteral)?.Value,
                (type as ArmStringLiteral)?.Value,
                (apiVersion as ArmStringLiteral)?.Value);
        }

        public ArmResourceName(string resourceNamespace, string type, string apiVersion)
        {
            Namespace = resourceNamespace;
            Type = type;
            ApiVersion = apiVersion;
        }

        public readonly string Namespace;

        public readonly string Type;

        public readonly string ApiVersion;
    }
}

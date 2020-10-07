
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace PSArm.Schema
{
    /// <summary>
    /// Loads DSL schemas from description files in a threadsafe way.
    /// Manages the details of how DSL descriptions are stored.
    /// </summary>
    public class DslLoader
    {
        /// <summary>
        /// The singleton instance of the DSL loader to be used by default
        /// by argument completers and ARM template commands.
        /// </summary>
        public static DslLoader Instance = new DslLoader(
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "schemas"));

        private readonly string _basePath;

        private readonly ConcurrentDictionary<ArmSchemaName, ArmProviderDslInfo> _dsls;

        /// <summary>
        /// Create a new DSL loader around a given schema directory.
        /// </summary>
        /// <param name="dirPath">The path to the directory where schema descriptions are stored.</param>
        public DslLoader(string dirPath)
        {
            _dsls = new ConcurrentDictionary<ArmSchemaName, ArmProviderDslInfo>();
            _basePath = dirPath;
        }

        /// <summary>
        /// Try to load a DSL schema by resource namespace.
        /// </summary>
        /// <param name="schemaName">The resource provider schema to load.</param>
        /// <param name="apiVersion">The API version of the resource provider to load.</param>
        /// <param name="dslInfo">The loaded DSL description object.</param>
        /// <returns>True when loading succeeded, false otherwise.</returns>
        public bool TryLoadDsl(string schemaName, string apiVersion, out ArmProviderDslInfo dslInfo)
        {
            try
            {
                dslInfo = LoadDsl(schemaName, apiVersion);
                return true;
            }
            catch
            {
                dslInfo = null;
                return false;
            }
        }

        /// <summary>
        /// Load a resource namespace DSL.
        /// </summary>
        /// <param name="schemaName">The resource namespace of the DSL to load.</param>
        /// <returns>The DSL description object of the resource namespace.</returns>
        public ArmProviderDslInfo LoadDsl(string schemaName, string apiVersion)
        {
            var schemaKey = new ArmSchemaName
            {
                ProviderName = schemaName,
                ApiVersion = apiVersion,
            };
            return _dsls.GetOrAdd(schemaKey, LoadSchemaFromFile);
        }

        public IReadOnlyList<string> ListSchemaProviders() => ListSchemaProviders(apiVersion: null);

        public IReadOnlyList<string> ListSchemaProviders(string apiVersion)
        {
            var providers = new List<string>();
            foreach (string schemaFilePath in Directory.GetFiles(_basePath))
            {
                string schemaFileName = Path.GetFileNameWithoutExtension(schemaFilePath);

                int underscoreIdx = schemaFileName.IndexOf('_');

                if (!string.IsNullOrEmpty(apiVersion) && schemaFileName.IndexOf(apiVersion, underscoreIdx + 1) < 0)
                {
                    if (schemaFileName.IndexOf(apiVersion, underscoreIdx + 1) < 0)
                    {
                        continue;
                    }
                }

                providers.Add(schemaFileName.Substring(0, underscoreIdx));
            }
            return providers;
        }

        public IReadOnlyList<string> ListSchemaVersions() => ListSchemaVersions(providerName: null);

        public IReadOnlyList<string> ListSchemaVersions(string providerName)
        {
            var versions = new List<string>();
            foreach (string schemaFilePath in Directory.GetFiles(_basePath))
            {
                string schemaFileName = Path.GetFileNameWithoutExtension(schemaFilePath);

                if (!string.IsNullOrEmpty(providerName) && !schemaFileName.StartsWith(providerName))
                {
                    continue;
                }

                int versionIdx = schemaFileName.IndexOf('_') + 1;
                versions.Add(schemaFileName.Substring(versionIdx));
            }
            return versions;
        }

        private ArmProviderDslInfo LoadSchemaFromFile(ArmSchemaName schemaName)
        {
            string path = Path.Combine(_basePath, $"{schemaName.ProviderName}_{schemaName.ApiVersion}.json");
            ArmDslProviderSchema schema = new DslSchemaReader().ReadProviderSchema(path);
            return new ArmProviderDslInfo(schema);
        }

        private class ArmSchemaName
        {
            public string ProviderName { get; set; }

            public string ApiVersion { get; set;  }

            public override bool Equals(object obj)
            {
                if (object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj is null
                    || !(obj is ArmSchemaName that))
                {
                    return false;
                }

                return string.Equals(ProviderName, that.ProviderName)
                    && string.Equals(ApiVersion, that.ApiVersion);
            }

            public override int GetHashCode()
            {
                // From https://stackoverflow.com/a/34006336
                unchecked
                {
                    int factor = 9176;
                    int hash = 1009;
                    hash = hash * factor + (ProviderName != null ? ProviderName.GetHashCode() : 0);
                    hash = hash * factor + (ApiVersion != null ? ApiVersion.GetHashCode() : 0);
                    return hash;
                }
            }
        }
    }
}
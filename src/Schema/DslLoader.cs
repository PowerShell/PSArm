
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private readonly ConcurrentDictionary<string, ArmDslInfo> _dsls;

        /// <summary>
        /// Create a new DSL loader around a given schema directory.
        /// </summary>
        /// <param name="dirPath">The path to the directory where schema descriptions are stored.</param>
        public DslLoader(string dirPath)
        {
            _dsls = new ConcurrentDictionary<string, ArmDslInfo>();
            _basePath = dirPath;
        }

        /// <summary>
        /// Try to load a DSL schema by resource namespace.
        /// </summary>
        /// <param name="schemaName">The resource namespace to load.</param>
        /// <param name="dslInfo">The loaded DSL description object.</param>
        /// <returns>True when loading succeeded, false otherwise.</returns>
        public bool TryLoadDsl(string schemaName, out ArmDslInfo dslInfo)
        {
            try
            {
                dslInfo = LoadDsl(schemaName);
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
        public ArmDslInfo LoadDsl(string schemaName)
        {
            return _dsls.GetOrAdd(schemaName, LoadSchemaFromFile);
        }

        /// <summary>
        /// List available schema names (resource namespaces).
        /// </summary>
        /// <returns>A list of ARM resource namespaces that have schemas available in the DSL.</returns>
        public IReadOnlyList<string> ListSchemas()
        {
            var schemas = new List<string>();
            foreach (string entry in Directory.GetFiles(_basePath))
            {
                schemas.Add(Path.GetFileNameWithoutExtension(entry));
            }
            return schemas;
        }

        private ArmDslInfo LoadSchemaFromFile(string schemaName)
        {
            string path = Path.Combine(_basePath, $"{schemaName}.json");
            DslSchema schema = new DslSchemaReader().ReadSchema(path);
            IReadOnlyDictionary<string, string> dslDefinitions = new DslScriptWriter().WriteDslDefinitions(schema);
            return new ArmDslInfo(schema, dslDefinitions);
        }
    }
}
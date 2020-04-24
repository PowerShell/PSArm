using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PSArm
{
    public class DslLoader
    {
        public static DslLoader Instance = new DslLoader(
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "dsls"));

        private readonly string _basePath;

        private readonly ConcurrentDictionary<string, ArmDslInfo> _dsls;

        public DslLoader(string dirPath)
        {
            _dsls = new ConcurrentDictionary<string, ArmDslInfo>();
            _basePath = dirPath;
        }

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

        public ArmDslInfo LoadDsl(string schemaName)
        {
            return _dsls.GetOrAdd(schemaName, LoadSchemaFromFile);
        }

        private ArmDslInfo LoadSchemaFromFile(string schemaName)
        {
            string path = Path.Combine(_basePath, $"{schemaName}.json");
            DslSchema schema = new DslSchemaReader().ReadSchema(path);
            IReadOnlyDictionary<string, string> dslDefinitions = new DslScriptWriter().WriteDslDefinitions(schema);
            return new ArmDslInfo(schema, dslDefinitions);
        }
    }

    public class ArmDslInfo
    {
        public ArmDslInfo(DslSchema schema, IReadOnlyDictionary<string, string> dslScripts)
        {
            Schema = schema;
            DslDefintions = dslScripts;
        }

        public DslSchema Schema { get; }

        public IReadOnlyDictionary<string, string> DslDefintions { get; }
    }
}
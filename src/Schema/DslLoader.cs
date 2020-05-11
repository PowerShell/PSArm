using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PSArm.Schema
{
    public class DslLoader
    {
        public static DslLoader Instance = new DslLoader(
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "schemas"));

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
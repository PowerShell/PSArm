using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace PSArm.Schema
{
    /// <summary>
    /// Reader object to read ARM DSL schemas from files.
    /// </summary>
    public class DslSchemaReader
    {
        private readonly JsonSerializer _jsonSerializer;

        /// <summary>
        /// Create a new DSL schema reader.
        /// </summary>
        public DslSchemaReader()
        {
            _jsonSerializer = new JsonSerializer()
            {
                Converters = { new DslSchemaConverter() },
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            };
        }

        /// <summary>
        /// Read an ARM resource DSL schema from a given file.
        /// </summary>
        /// <param name="path">The path to the file to read from.</param>
        /// <returns>The DSL schema object the file describes.</returns>
        public DslSchema ReadSchema(string path)
        {
            Dictionary<string, Dictionary<string, DslSchemaItem>> subschemas = null;
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var textReader = new StreamReader(file))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                subschemas = _jsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DslSchemaItem>>>(jsonReader);
            }

            string schemaNamspace = Path.GetFileNameWithoutExtension(path);

            return new DslSchema(schemaNamspace, subschemas);
        }
    }
}

using System.Collections.Generic;

namespace PSArm.Schema
{
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

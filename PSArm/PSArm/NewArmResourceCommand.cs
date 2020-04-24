using System.Collections.Generic;
using System.Management.Automation;

namespace PSArm
{
    [Alias("Resource")]
    [Cmdlet(VerbsCommon.New, "ArmResource")]
    public class NewArmResourceCommand : PSCmdlet
    {
        private readonly static char[] s_splitChar = new [] { '/' };

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter()]
        public string Location { get; set; }

        [Parameter()]
        public string ApiVersion { get; set; }

        [Parameter()]
        public string Type { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            string[] schemaNameParts = Name.Split(s_splitChar);
            ArmDslInfo dsl = DslLoader.Instance.LoadDsl(schemaNameParts[0]);
            var resourceDsl = ScriptBlock.Create(dsl.DslDefintions[schemaNameParts[1]]);
            InvokeCommand.InvokeScript(SessionState, resourceDsl);

            var dict = new Dictionary<string, ArmPropertyInstance>();

            foreach (PSObject result in InvokeCommand.InvokeScript(SessionState, Body))
            {
                if (result.BaseObject is ArmPropertyInstance armProperty)
                {
                    dict[armProperty.PropertyName] = armProperty;
                }
            }

            var resource = new ArmResource
            {
                ApiVersion = ApiVersion,
                Location = Location,
                Type = Type,
                Name = Name,
                Properties = dict,
            };

            WriteObject(resource);
        }
    }
}
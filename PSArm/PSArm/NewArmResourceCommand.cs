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

        [ValidateSet("WestUS", "WestUS2")]
        [Parameter()]
        public string Location { get; set; }

        [Parameter()]
        public string ApiVersion { get; set; }

        [ArgumentCompleter(typeof(ResourceArgumentCompleter))]
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

            var properties = new Dictionary<string, ArmPropertyInstance>();
            var subresources = new Dictionary<string, ArmResource>();

            foreach (PSObject result in InvokeCommand.InvokeScript(SessionState, Body))
            {
                switch (result.BaseObject)
                {
                    case ArmPropertyInstance armProperty:
                        properties[armProperty.PropertyName] = armProperty;
                        continue;

                    case ArmResource subresource:
                        subresources[subresource.Name] = subresource;
                        continue;
                }
            }

            var resource = new ArmResource
            {
                ApiVersion = ApiVersion,
                Location = Location,
                Type = Type,
                Name = Name,
                Properties = properties,
                Subresources = subresources,
            };

            WriteObject(resource);
        }
    }
}
using System.Collections.Generic;
using System.Management.Automation;

namespace PSArm
{
    [Alias("Resource")]
    [Cmdlet(VerbsCommon.New, "ArmResource")]
    public class NewArmResourceCommand : PSCmdlet
    {
        private readonly static char[] s_splitChar = new [] { '/' };

        internal static string SyntaxDescription = @"
Resource -Name <string> -Location <string> -ApiVersion <string> -Type <string> [-Body] <scriptblock>
";

        [Parameter(Position = 0, Mandatory = true)]
        public IArmExpression Name { get; set; }

        [Parameter()]
        public IArmExpression Location { get; set; }

        [Parameter()]
        public string ApiVersion { get; set; }

        [ArgumentCompleter(typeof(ResourceArgumentCompleter))]
        [Parameter()]
        public string Type { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            string[] schemaNameParts = Type.Split(s_splitChar);
            ArmDslInfo dsl = DslLoader.Instance.LoadDsl(schemaNameParts[0]);
            var resourceDsl = ScriptBlock.Create(dsl.DslDefintions[Type]);
            InvokeCommand.InvokeScript(SessionState, resourceDsl);

            var properties = new Dictionary<string, ArmPropertyInstance>();
            var subresources = new Dictionary<IArmExpression, ArmResource>();
            var dependsOns = new List<IArmExpression>();

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

                    case ArmDependsOn dependsOn:
                        dependsOns.Add(dependsOn.Value);
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
                DependsOn = dependsOns,
            };

            WriteObject(resource);
        }
    }
}
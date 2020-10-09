
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Completion;
using PSArm.Expression;
using PSArm.Schema;

namespace PSArm.Commands
{
    [Alias("Resource")]
    [Cmdlet(VerbsCommon.New, "ArmResource")]
    public class NewArmResourceCommand : PSCmdlet
    {
        internal static string SyntaxDescription = @"
Resource -Name <string> -Location <string> -ApiVersion <string> -Provider <string> -Type <string> [-Body] <scriptblock>
";

        [Parameter(Position = 0, Mandatory = true)]
        public IArmExpression Name { get; set; }

        [Parameter]
        public IArmExpression Location { get; set; }

        [Parameter]
        public IArmExpression Kind { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter]
        public string ApiVersion { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter]
        public string Provider { get; set; }

        [ArgumentCompleter(typeof(ArmResourceArgumentCompleter))]
        [Parameter]
        public string Type { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            // Try and define the functions needed to define the DSL at this point.
            // If we can't, we still allow things to proceed -- the user may be defining custom entries
            if (DslLoader.Instance.TryLoadDsl(Provider, ApiVersion, out ArmProviderDslInfo dsl))
            {
                var resourceDsl = ScriptBlock.Create(dsl.ScriptProducer.GetResourceScriptDefintion(Type));
                InvokeCommand.InvokeScript(SessionState, resourceDsl);
            }

            var properties = new Dictionary<string, ArmPropertyInstance>();
            var subresources = new Dictionary<IArmExpression, ArmResource>();
            var dependsOns = new List<IArmExpression>();
            ArmSku armSku = null;

            foreach (PSObject result in InvokeCommand.InvokeScript(SessionState, Body))
            {
                switch (result.BaseObject)
                {
                    case ArmPropertyArrayItem arrayItem:
                        if (!properties.TryGetValue(arrayItem.PropertyName, out ArmPropertyInstance arrayProperties))
                        {
                            arrayProperties = new ArmPropertyArray(arrayItem.PropertyName);
                            properties[arrayItem.PropertyName] = arrayProperties;
                        }

                        var arrayCollector = (ArmPropertyArray)arrayProperties;
                        arrayCollector.Items.Add(arrayItem);
                        continue;

                    case ArmPropertyInstance armProperty:
                        properties[armProperty.PropertyName] = armProperty;
                        continue;

                    case ArmResource subresource:
                        subresources[subresource.Name] = subresource;
                        continue;

                    case ArmDependsOn dependsOn:
                        dependsOns.Add(dependsOn.Value);
                        continue;

                    case ArmSku sku:
                        armSku = sku;
                        continue;
                }
            }

            var resource = new ArmResource
            {
                ApiVersion = ApiVersion,
                Location = Location,
                Type = $"{Provider}/{Type}",
                Name = Name,
                Properties = properties,
                Subresources = subresources,
                DependsOn = dependsOns,
                Sku = armSku,
            };

            WriteObject(resource);
        }
    }
}
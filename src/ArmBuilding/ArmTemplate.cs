using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    public class ArmTemplate
    {
        public ArmTemplate()
        {
            Resources = new List<ArmResource>();
            Outputs = new List<ArmOutput>();
        }

        public string Schema { get; set; } = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#";

        public Version ContentVersion { get; set; } = new Version(1, 0, 0, 0);

        public List<ArmResource> Resources { get; set; }

        public List<ArmOutput> Outputs { get; set; }

        public ArmParameter[] Parameters { get; set; }

        public ArmVariable[] Variables { get; set; }

        public JObject ToJson()
        {
            var jObj = new JObject
            {
                ["$schema"] = Schema,
                ["contentVersion"] = ContentVersion.ToString(),
            };

            if (Parameters != null && Parameters.Length != 0)
            {
                var parameters = new JObject();
                foreach (ArmParameter parameter in Parameters)
                {
                    parameters[parameter.Name] = parameter.ToJson();
                }
                jObj["parameters"] = parameters;
            }

            if (Variables != null && Variables.Length != 0)
            {
                var variables = new JObject();
                foreach (ArmVariable variable in Variables)
                {
                    variables[variable.Name] = variable.ToJson();
                }
                jObj["variables"] = variables;
            }

            if (Outputs != null && Outputs.Count != 0)
            {
                var outputs = new JObject();
                foreach (ArmOutput output in Outputs)
                {
                    outputs[output.Name.ToExpressionString()] = output.ToJson();
                }
                jObj["outputs"] = outputs;
            }

            var resources = new JArray();
            foreach (ArmResource resource in Resources)
            {
                resources.Add(resource.ToJson());
            }
            jObj["resources"] = resources;

            return jObj;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }

        public ArmTemplate Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            var outputs = new List<ArmOutput>();
            foreach (ArmOutput output in Outputs)
            {
                outputs.Add(output.Instantiate(parameters));
            }

            var resources = new List<ArmResource>();
            foreach (ArmResource resource in Resources)
            {
                resources.Add(resource.Instantiate(parameters));
            }

            List<ArmVariable> variables = null;
            if (Variables != null)
            {
                variables = new List<ArmVariable>();
                foreach (ArmVariable variable in Variables)
                {
                    variables.Add((ArmVariable)variable.Instantiate(parameters));
                }
            }

            return new ArmTemplate
            {
                ContentVersion = ContentVersion,
                Schema = Schema,
                Outputs = outputs,
                Resources = resources,
                Variables = variables?.ToArray(),
            };
        }
    }
}
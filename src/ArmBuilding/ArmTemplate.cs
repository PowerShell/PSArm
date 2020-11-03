
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSArm.Expression;

namespace PSArm.ArmBuilding
{
    /// <summary>
    /// A full ARM template. May be parameterized or instantiated.
    /// </summary>
    public class ArmTemplate : IArmElement
    {
        /// <summary>
        /// Create a new blank ARM template.
        /// </summary>
        public ArmTemplate()
        {
            Resources = new List<ArmResource>();
            Outputs = new List<ArmOutput>();
        }

        /// <summary>
        /// The JSON schema path for this template.
        /// </summary>
        public string Schema { get; set; } = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#";

        /// <summary>
        /// The content version of this template.
        /// </summary>
        public Version ContentVersion { get; set; } = new Version(1, 0, 0, 0);

        /// <summary>
        /// A list of resources used in the template.
        /// </summary>
        public List<ArmResource> Resources { get; set; }

        /// <summary>
        /// A list of outputs expressed by the template.
        /// </summary>
        public List<ArmOutput> Outputs { get; set; }

        /// <summary>
        /// ARM parameters on this template that must be passed in to instantiate it.
        /// </summary>
        public List<ArmParameter> Parameters { get; set; }

        /// <summary>
        /// ARM variables on this template.
        /// </summary>
        public List<ArmVariable> Variables { get; set; }

        /// <summary>
        /// Render the template as ARM template JSON.
        /// </summary>
        /// <returns>A JSON object representing the JSON form of this template.</returns>
        public JToken ToJson()
        {
            var jObj = new JObject
            {
                ["$schema"] = Schema,
                ["contentVersion"] = ContentVersion.ToString(),
            };

            if (Parameters != null && Parameters.Count != 0)
            {
                var parameters = new JObject();
                foreach (ArmParameter parameter in Parameters)
                {
                    parameters[parameter.Name] = parameter.ToJson();
                }
                jObj["parameters"] = parameters;
            }

            if (Variables != null && Variables.Count != 0)
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

            if (Resources != null && Resources.Count != 0)
            {
                var resources = new JArray();
                foreach (ArmResource resource in Resources)
                {
                    resources.Add(resource.ToJson());
                }
                jObj["resources"] = resources;
            }

            return jObj;
        }

        /// <summary>
        /// Show this template as an ARM template JSON string.
        /// </summary>
        /// <returns>A string capturing the ARM template in ARM template JSON.</returns>
        public override string ToString()
        {
            return ToJson().ToString();
        }

        /// <summary>
        /// Instantiate parameters on the ARM template with the given ARM template values.
        /// </summary>
        /// <param name="parameters">Values to instantiate ARM parameters with.</param>
        /// <returns>A copy of the ARM template with all given parameter values instantiated.</returns>
        public ArmTemplate Instantiate(IReadOnlyDictionary<string, IArmValue> parameters)
        {
            // No parameters to instantiate, so save the trouble
            if (Parameters == null)
            {
                return this;
            }

            // Go through given parameters and add any that require default values
            Dictionary<string, IArmValue> defaultParametersToUse = null;
            foreach (ArmParameter parameter in Parameters)
            {
                if (!parameters.ContainsKey(parameter.Name)
                    && parameter.DefaultValue != null)
                {
                    if (defaultParametersToUse == null)
                    {
                        defaultParametersToUse = new Dictionary<string, IArmValue>();
                    }

                    defaultParametersToUse[parameter.Name] = parameter.DefaultValue;
                }
            }

            // If we need to use default parameters,
            // add the existing parameters to the dictionary
            // and use that instead
            if (defaultParametersToUse != null)
            {
                foreach (KeyValuePair<string, IArmValue> givenParameter in parameters)
                {
                    defaultParametersToUse[givenParameter.Key] = givenParameter.Value;
                }

                parameters = defaultParametersToUse;
            }

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
                Variables = variables,
            };
        }
    }
}
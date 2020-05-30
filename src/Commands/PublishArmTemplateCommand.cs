
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using Newtonsoft.Json;
using PSArm.ArmBuilding;
using PSArm.Expression;
using PSArm.Internal;

namespace PSArm.Commands
{

    [Cmdlet(VerbsData.Publish, "ArmTemplate", DefaultParameterSetName = "OutFile")]
    public class PublishArmTemplateCommand : PSCmdlet, IDynamicParameters
    {
        private const string ParamSet_Hashtable = "ParamsAsHashtable";
        private const string ParamSet_DynamicParams = "ParamsAsDynamicParams";

        private RuntimeDefinedParameterDictionary _dynamicParameters;

        [Parameter(Position = 0, Mandatory = true)]
        public ArmTemplate Template { get; set; }

        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty()]
        public string OutFile { get; set; }

        [Parameter(Position = 2, ParameterSetName = ParamSet_Hashtable)]
        public Hashtable Parameters { get; set; }

        [Parameter()]
        public SwitchParameter PassThru { get; set; }

        protected override void EndProcessing()
        {
            var parameterValues = new Dictionary<string, IArmExpression>();

            if (Parameters != null)
            {
                foreach (DictionaryEntry entry in Parameters)
                {
                    parameterValues[entry.Key.ToString()] = ArmTypeConversion.Convert(entry.Value);
                }
            }
            else if (_dynamicParameters != null)
            {
                foreach (KeyValuePair<string, RuntimeDefinedParameter> entry in _dynamicParameters)
                {
                    parameterValues[entry.Key] = ArmTypeConversion.Convert(entry.Value.Value);
                }
            }

            ArmTemplate instantiatedTemplate = Template.Instantiate(parameterValues);

            if (OutFile != null)
            {
                using (FileStream fileStream = File.Open(OutFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var textWriter = new StreamWriter(fileStream))
                using (var jsonWriter = new JsonTextWriter(textWriter){ Formatting = Formatting.Indented })
                {
                    instantiatedTemplate.ToJson().WriteTo(jsonWriter);
                }
            }

            if (PassThru)
            {
                WriteObject(instantiatedTemplate);
            }
        }

        public object GetDynamicParameters()
        {
            if (Template.Parameters == null
                || Template.Parameters.Length == 0
                || ParameterSetName.Is(ParamSet_Hashtable))
            {
                return null;
            }

            var parameters = new RuntimeDefinedParameterDictionary();
            foreach (ArmParameter armParameter in Template.Parameters)
            {
                var attributes = new Collection<Attribute>();

                if (armParameter.AllowedValues != null)
                {
                    var allowedValues = new List<string>();
                    foreach (object v in armParameter.AllowedValues)
                    {
                        allowedValues.Add(v.ToString());
                    }
                    attributes.Add(new ValidateSetAttribute(allowedValues.ToArray()));
                }

                if (armParameter.DefaultValue != null)
                {
                    attributes.Add(new ParameterAttribute{ ParameterSetName = ParamSet_DynamicParams });

                    parameters[armParameter.Name] = new RuntimeDefinedParameter(
                        armParameter.Name,
                        armParameter.Type,
                        attributes)
                    {
                        Value = armParameter.DefaultValue,
                    };
                }
                else
                {
                    attributes.Add(new ParameterAttribute{ Mandatory = true, ParameterSetName = ParamSet_DynamicParams });

                    parameters[armParameter.Name] = new RuntimeDefinedParameter(
                        armParameter.Name,
                        armParameter.Type,
                        attributes);
                }

            }

            return _dynamicParameters = parameters;
        }
    }
}
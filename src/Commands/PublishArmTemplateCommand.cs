using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Expression;

namespace PSArm.Commands
{

    [Cmdlet(VerbsData.Publish, "ArmTemplate")]
    public class PublishArmTemplateCommand : PSCmdlet, IDynamicParameters
    {
        private RuntimeDefinedParameterDictionary _dynamicParameters;

        [Parameter(Position = 0, Mandatory = true)]
        public ArmTemplate Template { get; set; }

        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "PassByHashtable")]
        public Hashtable[] Parameters { get; set; }

        protected override void EndProcessing()
        {
            Dictionary<string, ArmLiteral> parameterValues = null;

            if (Parameters != null)
            {
                foreach (Hashtable parameterSetting in Parameters)
                {
                    parameterValues = new Dictionary<string, ArmLiteral>();
                    foreach (DictionaryEntry entry in parameterSetting)
                    {
                        parameterValues[entry.Key.ToString()] = (ArmLiteral)ArmTypeConversion.Convert(entry.Value);
                    }
                    WriteObject(Template.Instantiate(parameterValues));
                }
                return;
            }

            parameterValues = new Dictionary<string, ArmLiteral>();
            foreach (KeyValuePair<string, RuntimeDefinedParameter> entry in _dynamicParameters)
            {
                parameterValues[entry.Key] = (ArmLiteral)ArmTypeConversion.Convert(entry.Value.Value);
            }
            WriteObject(Template.Instantiate(parameterValues));
        }

        public object GetDynamicParameters()
        {
            if (Template.Parameters == null
                || Template.Parameters.Length == 0
                || string.Equals(ParameterSetName, "PassByHashtable", StringComparison.OrdinalIgnoreCase))
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
                    attributes.Add(new ParameterAttribute{ ParameterSetName = "PassByDynamicParams" });

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
                    attributes.Add(new ParameterAttribute{ Mandatory = true, ParameterSetName = "PassByDynamicParams" });

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
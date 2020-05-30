
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using PSArm.ArmBuilding;
using PSArm.Expression;

namespace PSArm.Commands.ArmBuilding
{
    [Alias("Block")]
    [Cmdlet(VerbsCommon.New, "ArmPropertyBlock")]
    public class NewArmPropertyBlockCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter()]
        public Hashtable Parameters { get; set; }

        [Parameter()]
        public Hashtable Properties { get; set; }

        [Parameter(Position = 1)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            var result = CreatePropertyObject();

            if (Parameters != null)
            {
                foreach (DictionaryEntry parameter in Parameters)
                {
                    if (parameter.Value != null)
                    {
                        result.Parameters[parameter.Key.ToString()] = ArmTypeConversion.Convert(parameter.Value);
                    }
                }
            }

            if (Properties != null)
            {
                foreach (DictionaryEntry property in Properties)
                {
                    if (property.Value != null)
                    {
                        result.Properties[property.Key.ToString()] = new ArmPropertyValue(property.Key.ToString(), ArmTypeConversion.Convert(property.Value));
                    }
                }
            }

            Dictionary<string, List<ArmPropertyArrayItem>> arrayItems = null;
            if (Body != null)
            {
                foreach (PSObject bodyOutput in InvokeCommand.InvokeScript(SessionState, Body))
                {
                    switch (bodyOutput.BaseObject)
                    {
                        case ArmPropertyValue propertyValue:
                            result.Properties[propertyValue.PropertyName] = propertyValue;
                            continue;

                        case ArmParameterizedProperty parameterizedProperty:
                            result.Properties[parameterizedProperty.PropertyName] = parameterizedProperty;
                            continue;

                        case ArmPropertyArrayItem arrayItem:
                            if (arrayItems == null)
                            {
                                arrayItems = new Dictionary<string, List<ArmPropertyArrayItem>>();
                            }

                            if (!arrayItems.TryGetValue(arrayItem.PropertyName, out List<ArmPropertyArrayItem> list))
                            {
                                list = new List<ArmPropertyArrayItem>();
                                arrayItems[arrayItem.PropertyName] = list;
                            }

                            list.Add(arrayItem);
                            continue;

                        case ArmPropertyObject propertyObject:
                            result.Properties[propertyObject.PropertyName] = propertyObject;
                            continue;
                    }
                }
            }

            if (arrayItems != null)
            {
                foreach (KeyValuePair<string, List<ArmPropertyArrayItem>> arrayProperty in arrayItems)
                {
                    var property = ArmPropertyArray.FromArrayItems(arrayProperty.Value);
                    result.Properties[property.PropertyName] = property;
                }
            }

            WriteObject(result);
        }
        
        protected virtual ArmPropertyObject CreatePropertyObject()
        {
            return new ArmPropertyObject(Name);
        }

        private static string UnPascal(string s)
        {
            return char.IsUpper(s[0])
                ? char.ToLower(s[0]) + s.Substring(1)
                : s;

        }
    }

}
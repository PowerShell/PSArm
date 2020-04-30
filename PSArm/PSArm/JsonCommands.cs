using System.Collections.Generic;
using System.Management.Automation;

namespace PSArm
{
    [Alias("Value")]
    [Cmdlet(VerbsCommon.New, "ArmValue")]
    public class NewArmValueCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public IArmExpression Value { get; set; }

        protected override void EndProcessing()
        {
            WriteObject(new ArmPropertyValue(Name, Value));
        }
    }

    [Alias("Composite")]
    [Cmdlet(VerbsCommon.New, "ArmCompositeValue")]
    public class NewArmCompositeValue : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public Dictionary<string, object> Parameters { get; set; }

        protected override void EndProcessing()
        {
            var result = new ArmParameterizedProperty(Name);
            foreach (KeyValuePair<string, object> parameter in Parameters)
            {
                result.Parameters[parameter.Key] = ArmTypeConversion.Convert(parameter.Value);
            }
            WriteObject(result);
        }
    }

    [Alias("Block")]
    [Cmdlet(VerbsCommon.New, "ArmPropertyBlock")]
    public class NewArmPropertyBlockCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public Dictionary<string, object> Parameters { get; set; }

        [Parameter(Position = 2, Mandatory = true)]
        public ScriptBlock Body { get; set; }

        protected override void EndProcessing()
        {
            var result = CreatePropertyObject();

            foreach (KeyValuePair<string, object> parameter in Parameters)
            {
                if (parameter.Key == "Body")
                {
                    continue;
                }

                result.Parameters[UnPascal(parameter.Key)] = ArmTypeConversion.Convert(parameter.Value);
            }

            Dictionary<string, List<ArmPropertyArrayItem>> arrayItems = null;
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

    [Alias("ArrayItem")]
    [Cmdlet(VerbsCommon.New, "ArmArrayItem")]
    public class NewArmArrayItemCommand : NewArmPropertyBlockCommand
    {
        protected override ArmPropertyObject CreatePropertyObject()
        {
            return new ArmPropertyArrayItem(Name);
        }
    }
}
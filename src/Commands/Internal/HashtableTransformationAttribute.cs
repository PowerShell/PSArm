
// Copyright (c) Microsoft Corporation.

using System.Collections;
using System.Management.Automation;

namespace PSArm.Commands.Internal
{
    public class HashtableTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            switch (inputData)
            {
                case null:
                    return null;

                case PSObject psObject:
                    return TransformPSObject(psObject);

                case Hashtable parameterHashtable:
                    return parameterHashtable;

                default:
                    throw new ArgumentTransformationMetadataException($"Unable to transform type '{inputData.GetType()}' to a hashtable");
            }
        }

        private Hashtable TransformPSObject(PSObject psObject)
        {
            var hashtable = new Hashtable();
            foreach (PSPropertyInfo property in psObject.Properties)
            {
                hashtable[property.Name] = property.Value;
            }
            return hashtable;
        }
    }
}

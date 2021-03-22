
// Copyright (c) Microsoft Corporation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

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

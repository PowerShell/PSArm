
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Builders;
using PSArm.Templates.Primitives;
using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace PSArm.Commands.Internal
{
    public abstract class PSArmKeywordCommand : PSCmdlet
    {
        protected Collection<PSObject> InvokeBody(ScriptBlock body) => InvokeCommand.InvokeScript(useLocalScope: false, body, input: null, args: null);

        protected TArmObject AggregateArmObject<TArmObject>(
            ScriptBlock body)
                where TArmObject : ArmObject, new()
            => AggregateArmObject(new ConstructingArmBuilder<TArmObject>(), body);

        protected TArmObject AggregateArmObject<TArmObject>(
            ArmBuilder<TArmObject> builder,
            ScriptBlock body)
                where TArmObject : ArmObject
        {
            foreach (PSObject output in InvokeBody(body))
            {
                if (output.BaseObject is ArmEntry armEntry)
                {
                    builder.AddEntry(armEntry);
                }
            }
            return builder.Build();
        }

        protected ArmArray<TArmElement> AggregateArmArray<TArmElement>(
            ScriptBlock body)
                where TArmElement : ArmElement
        {
            var array = new ArmArray<TArmElement>();
            foreach (PSObject output in InvokeBody(body))
            {
                if (output.BaseObject is TArmElement element)
                {
                    array.Add(element);
                }
            }
            return array;
        }

        protected void WriteArmArrayElement(ScriptBlock body) => WriteArmArrayElement<ArmElement>(body);

        protected void WriteArmArrayElement<TArmElement>(
            ScriptBlock body)
                where TArmElement : ArmElement
        {
            WriteObject(AggregateArmArray<TArmElement>(body));
        }

        protected void WriteArmObjectElement(ScriptBlock body) => WriteArmObjectElement<ArmObject>(body);

        protected void WriteArmObjectElement<TArmObject>(
            ScriptBlock body)
                where TArmObject : ArmObject, new()
        {
            WriteObject(AggregateArmObject<TArmObject>(body));
        }

        protected void WriteArmObjectElement<TArmObject>(
            ArmBuilder<TArmObject> builder,
            ScriptBlock body)
                where TArmObject : ArmObject
        {
            WriteObject(AggregateArmObject(builder, body));
        }

        protected void WriteArmValueEntry(
            IArmString key,
            ArmElement value,
            bool isArrayElement = false)
        {
            WriteObject(new ArmEntry(key, value, isArrayElement));
        }

        protected void WriteArmArrayEntry<TArmElement>(
            IArmString key,
            ScriptBlock body,
            bool isArrayElement = false)
                where TArmElement : ArmElement
        {
            WriteObject(new ArmEntry(key, AggregateArmArray<TArmElement>(body), isArrayElement));
        }

        protected void WriteArmObjectEntry<TArmObject>(
            IArmString key,
            ScriptBlock body,
            bool isArrayElement = false)
                where TArmObject : ArmObject, new()
        {
            WriteObject(
                new ArmEntry(
                    key,
                    AggregateArmObject<TArmObject>(body),
                    isArrayElement));
        }

        protected void WriteArmObjectEntry<TArmObject>(
            ArmBuilder<TArmObject> objectBuilder,
            IArmString key,
            ScriptBlock body,
            bool isArrayElement = false)
                where TArmObject : ArmObject
        {
            WriteObject(
                new ArmEntry(
                    key,
                    AggregateArmObject(objectBuilder, body),
                    isArrayElement));
        }

        protected void ThrowTerminatingError(
            Exception exception,
            string errorId,
            ErrorCategory errorCategory,
            object target = null)
        {
            ThrowTerminatingError(
                new ErrorRecord(
                    exception,
                    errorId,
                    errorCategory,
                    target));
        }
    }
}

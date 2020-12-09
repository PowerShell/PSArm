using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;

namespace PSArm.Templates.Builders
{
    public class ArmBuilder<TObject> where TObject : ArmObject
    {
        private readonly TObject _armObject;

        public ArmBuilder(TObject armObject)
        {
            _armObject = armObject;
        }

        public ArmBuilder<TObject> AddEntry(ArmEntry entry)
        {
            return entry.IsArrayElement
                ? AddArrayElement(entry.Key, entry.Value)
                : AddSingleElement(entry.Key, entry.Value);
        }

        public ArmBuilder<TObject> AddSingleElement(IArmString key, ArmElement value)
        {
            _armObject.Add(key, value);
            return this;
        }

        public ArmBuilder<TObject> AddArrayElement(IArmString key, ArmElement value)
        {
            if (_armObject.TryGetValue(key, out ArmElement existingElement))
            {
                if (!(existingElement is IList<ArmElement> existingArray))
                {
                    throw new InvalidOperationException($"Non-array entry already exists for key '{key}'");
                }

                existingArray.Add(value);
                return this;
            }

            _armObject[key] = new ArmArray()
            {
                value
            };
            return this;
        }

        public TObject Build()
        {
            return _armObject;
        }
    }
}

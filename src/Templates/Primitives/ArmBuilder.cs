using System;

namespace PSArm.Templates.Primitives
{
    public class ArmBuilder<T> where T : ArmObject
    {
        private readonly T _armObject;

        public ArmBuilder(T armObject)
        {
            _armObject = armObject;
        }

        public ArmBuilder<T> AddEntry(ArmEntry entry)
        {
            return entry.IsArrayElement
                ? AddArrayElement(entry.Key, entry.Value)
                : AddSingleElement(entry.Key, entry.Value);
        }

        public ArmBuilder<T> AddSingleElement(IArmString key, ArmElement value)
        {
            _armObject.Add(key, value);
            return this;
        }

        public ArmBuilder<T> AddArrayElement(IArmString key, ArmElement value)
        {
            if (_armObject.TryGetValue(key, out ArmElement existingElement))
            {
                if (!(existingElement is ArmArray existingArray))
                {
                    throw new InvalidOperationException($"Non-array entry already exists for key '{key}'");
                }

                existingArray.Add(value);
                return this;
            }

            _armObject[key] = new ArmArray
            {
                value
            };
            return this;
        }

        public T Build()
        {
            return _armObject;
        }
    }
}

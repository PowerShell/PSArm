using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace PSArm.Templates.Primitives
{
    public class ArmObject<TArmValue> : ArmElement, IDictionary<IArmString, TArmValue> where TArmValue : ArmElement
    {
        private readonly Dictionary<IArmString, TArmValue> _dict;

        public ArmObject()
        {
            _dict = new Dictionary<IArmString, TArmValue>();
        }

        public override JToken ToJson()
        {
            var jObj = new JObject();
            foreach (KeyValuePair<IArmString, TArmValue> entry in this)
            {
                jObj[entry.Key.ToString()] = entry.Value.ToJson();
            }
            return jObj;
        }

        public TArmValue this[IArmString key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<IArmString> Keys => _dict.Keys;

        public ICollection<TArmValue> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public void Add(IArmString key, TArmValue value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<IArmString, TArmValue> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<IArmString, TArmValue> item)
        {
            return ((IDictionary<IArmString, TArmValue>)_dict).Contains(item);
        }

        public bool ContainsKey(IArmString key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<IArmString, TArmValue>[] array, int arrayIndex)
        {
            ((IDictionary<IArmString, TArmValue>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<IArmString, TArmValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(IArmString key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<IArmString, TArmValue> item)
        {
            return ((IDictionary<IArmString, TArmValue>)_dict).Remove(item);
        }

        public bool TryGetValue(IArmString key, out TArmValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ArmObject : ArmObject<ArmElement>
    {
    }
}

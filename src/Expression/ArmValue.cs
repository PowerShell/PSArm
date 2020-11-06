using Newtonsoft.Json.Linq;
using PSArm.ArmBuilding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PSArm.Expression
{
    public interface IArmValue : IArmElement
    {
        /// <summary>
        /// Copy the ARM expression with any ARM parameters instantiated with given values.
        /// </summary>
        /// <param name="parameters">The values to instantiate parameters with.</param>
        /// <returns></returns>
        IArmValue Instantiate(IReadOnlyDictionary<string, IArmValue> parameters);
    }

    public class ArmObject : IArmValue, IDictionary<string, IArmValue>
    {
        private readonly Dictionary<string, IArmValue> _dict;

        public ArmObject()
        {
            _dict = new Dictionary<string, IArmValue>();
        }

        public IArmValue this[string key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<IArmValue> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public void Add(string key, IArmValue value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, IArmValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, IArmValue> item)
        {
            return ((IDictionary<string, IArmValue>)_dict).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, IArmValue>[] array, int arrayIndex)
        {
            ((IDictionary<string, IArmValue>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, IArmValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public IArmValue Instantiate(IReadOnlyDictionary<string, IArmValue> parameters)
        {
            var obj = new ArmObject();
            foreach (KeyValuePair<string, IArmValue> entry in _dict)
            {
                obj[entry.Key] = entry.Value.Instantiate(parameters);
            }
            return obj;
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<string, IArmValue> item)
        {
            return ((IDictionary<string, IArmValue>)_dict).Remove(item);
        }

        public JToken ToJson()
        {
            var jObj = new JObject();
            foreach (KeyValuePair<string, IArmValue> item in this)
            {
                jObj[item.Key] = item.Value.ToJson();
            }
            return jObj;
        }

        public bool TryGetValue(string key, out IArmValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }
    }

    public class ArmArray : IArmValue, IList<IArmValue>
    {
        private readonly List<IArmValue> _list;

        public ArmArray()
        {
            _list = new List<IArmValue>();
        }

        public IArmValue this[int index] { get => _list[index]; set => _list[index] = value; }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(IArmValue item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(IArmValue item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(IArmValue[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<IArmValue> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(IArmValue item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, IArmValue item)
        {
            _list.Insert(index, item);
        }

        public IArmValue Instantiate(IReadOnlyDictionary<string, IArmValue> parameters)
        {
            var arr = new ArmArray();
            foreach (IArmValue element in this)
            {
                arr.Add(element.Instantiate(parameters));
            }
            return arr;
        }

        public bool Remove(IArmValue item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public JToken ToJson()
        {
            var jArr = new JArray();
            foreach (IArmValue item in this)
            {
                jArr.Add(item.ToJson());
            }
            return jArr;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}

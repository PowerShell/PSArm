
// Copyright (c) Microsoft Corporation.

using Newtonsoft.Json.Linq;
using PSArm.Templates.Visitors;
using PSArm.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public class ArmObject : ArmElement, IDictionary<IArmString, ArmElement>, IReadOnlyDictionary<IArmString, ArmElement>
    {
        private readonly Dictionary<IArmString, ArmElement> _dict;

        public ArmObject()
        {
            _dict = new Dictionary<IArmString, ArmElement>();
        }

        public ArmElement this[IArmString key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<IArmString> Keys => _dict.Keys;

        public ICollection<ArmElement> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        IEnumerable<IArmString> IReadOnlyDictionary<IArmString, ArmElement>.Keys => Keys;

        IEnumerable<ArmElement> IReadOnlyDictionary<IArmString, ArmElement>.Values => Values;

        public void Add(IArmString key, ArmElement value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<IArmString, ArmElement> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<IArmString, ArmElement> item)
        {
            return ((IDictionary<IArmString, ArmElement>)_dict).Contains(item);
        }

        public bool ContainsKey(IArmString key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<IArmString, ArmElement>[] array, int arrayIndex)
        {
            ((IDictionary<IArmString, ArmElement>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<IArmString, ArmElement>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(IArmString key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<IArmString, ArmElement> item)
        {
            return ((IDictionary<IArmString, ArmElement>)_dict).Remove(item);
        }

        public bool TryGetValue(IArmString key, out ArmElement value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitObject(this);

        protected ArmElement GetElementOrNull(IArmString key)
        {
            return TryGetValue(key, out ArmElement value)
                ? value
                : null;
        }
    }

    public class ArmObject<TValue> : ArmObject, IDictionary<IArmString, TValue>, IReadOnlyDictionary<IArmString, TValue> where TValue : ArmElement
    {
        private IDictionary<IArmString, ArmElement> This => this;

        TValue IDictionary<IArmString, TValue>.this[IArmString key]
        {
            get => (TValue)This[key];
            set => This[key] = value;
        }

        ICollection<TValue> IDictionary<IArmString, TValue>.Values => This.Values.Cast<TValue>().ToArray();

        IEnumerable<IArmString> IReadOnlyDictionary<IArmString, TValue>.Keys => This.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<IArmString, TValue>.Values
        {
            get
            {
                foreach (TValue value in Values)
                {
                    yield return value;
                }
            }
        }

        TValue IReadOnlyDictionary<IArmString, TValue>.this[IArmString key] => ((IDictionary<IArmString, TValue>)this)[key];

        public void Add(IArmString key, TValue value)
        {
            This.Add(key, value);
        }

        public void Add(KeyValuePair<IArmString, TValue> item)
        {
            This.Add(new KeyValuePair<IArmString, ArmElement>(item.Key, item.Value));
        }

        public bool Contains(KeyValuePair<IArmString, TValue> item)
        {
            return This.Contains(new KeyValuePair<IArmString, ArmElement>(item.Key, item.Value));
        }

        public void CopyTo(KeyValuePair<IArmString, TValue>[] array, int arrayIndex)
        {
            IEnumerator<KeyValuePair<IArmString, TValue>> enumerator = ((IEnumerable<KeyValuePair<IArmString, TValue>>)this).GetEnumerator();
            for (int i = arrayIndex; enumerator.MoveNext(); i++)
            {
                array[i] = enumerator.Current;
            }
        }

        public bool Remove(KeyValuePair<IArmString, TValue> item)
        {
            return This.Remove(new KeyValuePair<IArmString, ArmElement>(item.Key, item.Value));
        }

        public bool TryGetValue(IArmString key, out TValue value)
        {
            if (!This.TryGetValue(key, out ArmElement element))
            {
                value = default;
                return false;
            }

            value = (TValue)element;
            return true;
        }

        IEnumerator<KeyValuePair<IArmString, TValue>> IEnumerable<KeyValuePair<IArmString, TValue>>.GetEnumerator()
        {
            foreach (KeyValuePair<IArmString, ArmElement> entry in This)
            {
                yield return new KeyValuePair<IArmString, TValue>(entry.Key, (TValue)entry.Value);
            }
        }

        protected new TValue GetElementOrNull(IArmString key)
        {
            return TryGetValue(key, out TValue value)
                ? value
                : default;
        }
    }
}

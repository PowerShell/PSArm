using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace PSArm.Templates.Primitives
{
    public class ArmArray<TArmItem> : ArmElement, IList<TArmItem> where TArmItem : ArmElement
    {
        private readonly List<TArmItem> _items;

        public ArmArray()
        {
            _items = new List<TArmItem>();
        }

        public TArmItem this[int index] { get => _items[index]; set => _items[index] = value; }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public override JToken ToJson()
        {
            var jArr = new JArray();
            foreach (TArmItem item in this)
            {
                jArr.Add(item.ToJson());
            }
            return jArr;
        }

        public void Add(TArmItem item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(TArmItem item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(TArmItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TArmItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(TArmItem item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, TArmItem item)
        {
            _items.Insert(index, item);
        }

        public bool Remove(TArmItem item)
        {
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ArmArray : ArmArray<ArmElement>
    {
    }
}

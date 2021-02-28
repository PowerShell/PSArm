
// Copyright (c) Microsoft Corporation.

using PSArm.Templates.Visitors;
using PSArm.Types;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace PSArm.Templates.Primitives
{
    [TypeConverter(typeof(ArmElementConverter))]
    public class ArmArray : ArmElement, IList<ArmElement>, IReadOnlyList<ArmElement>
    {
        private readonly List<ArmElement> _items;

        public ArmArray()
        {
            _items = new List<ArmElement>();
        }

        public ArmElement this[int index] { get => _items[index]; set => _items[index] = value; }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(ArmElement item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(ArmElement item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(ArmElement[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ArmElement> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(ArmElement item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, ArmElement item)
        {
            _items.Insert(index, item);
        }

        public bool Remove(ArmElement item)
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

        public override TResult Visit<TResult>(IArmVisitor<TResult> visitor) => visitor.VisitArray(this);
    }

    public class ArmArray<TElement> : ArmArray, IList<TElement>, IReadOnlyList<TElement> where TElement : ArmElement
    {
        private ArmArray This => this;

        TElement IReadOnlyList<TElement>.this[int index] => ((IList<TElement>)this)[index];

        TElement IList<TElement>.this[int index]
        { 
            get => (TElement)This[index];
            set => This[index] = value;
        }

        public void Add(TElement item)
        {
            This.Add(item);
        }

        public bool Contains(TElement item)
        {
            return This.Contains(item);
        }

        public void CopyTo(TElement[] array, int arrayIndex)
        {
            IEnumerator<TElement> enumerator = ((IEnumerable<TElement>)this).GetEnumerator();
            for (int i = arrayIndex; enumerator.MoveNext(); i++)
            {
                array[i] = enumerator.Current;
            }
        }

        public int IndexOf(TElement item)
        {
            return This.IndexOf(item);
        }

        public void Insert(int index, TElement item)
        {
            This.Insert(index, item);
        }

        public bool Remove(TElement item)
        {
            return This.Remove(item);
        }

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
        {
            foreach (TElement element in This)
            {
                yield return element;
            }
        }
    }
}

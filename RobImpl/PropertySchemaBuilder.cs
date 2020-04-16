using RobImpl.ArmSchema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace RobImpl
{
    public class Property
    {
        public Property(string name, JsonSchemaType type, bool required)
        {
            Name = name;
            Type = type;
            Required = required;
        }

        public string Name { get; }

        public JsonSchemaType Type { get; }

        public bool Required { get; }
    }

    public class ObjectProperty : Property
    {
        public ObjectProperty(string name, bool required)
            : base(name, JsonSchemaType.Object, required)
        {
        }

        public PropertyTable Body { get; set; }
    }

    public class PropertyTable : IDictionary<string, Property>
    {
        public PropertyTable()
        {
            _dict = new Dictionary<string, Property>();
        }

        private readonly Dictionary<string, Property> _dict;

        public Property this[string key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<Property> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public void Add(string key, Property value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, Property> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, Property> item)
        {
            return _dict.TryGetValue(item.Key, out Property value)
                && value == item.Value;
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, Property>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<string, Property> entry in this)
            {
                array[arrayIndex] = entry;
                arrayIndex++;
            }
        }

        public IEnumerator<KeyValuePair<string, Property>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<string, Property> item)
        {
            return Contains(item)
                && Remove(item.Key);
        }

        public bool TryGetValue(string key, out Property value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }
    }

    public class PropertySchemaBuilder
    {
        public Dictionary<string, PropertyTable> BuildPropertyHierarchy(ArmObjectSchema topLevelObject)
        {
            ArmJsonSchema[] resourceSchemas = ((ArmOneOfCombinator)((ArmListSchema)topLevelObject.Properties["resources"]).Items).OneOf;

            return new Dictionary<string, PropertyTable>();
        }

        private PropertyTable GetPropertyTable(ArmObjectSchema obj)
        {
            var pt = new PropertyTable();
            foreach (KeyValuePair<string, ArmJsonSchema> entry in obj.Properties)
            {
                pt[entry.Key] = GetProperty(entry.Value, entry.Key, required: obj.Required != null && obj.Required.Contains(entry.Key));
            }
            return pt;
        }
    }
}

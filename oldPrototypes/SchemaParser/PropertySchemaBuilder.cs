
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using Newtonsoft.Json;
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
        public Property(string name, JsonSchemaType? type, bool required)
        {
            Name = name;
            Type = type;
            Required = required;
        }

        public string Name { get; }

        public JsonSchemaType? Type { get; }

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
        private char[] s_propertyNameSeparator = new[] { '/' };

        public Dictionary<string, PropertyTable> BuildPropertyHierarchy(ArmJsonSchema topLevelObject)
        {
            var foldedSchema = (ArmObjectSchema)SchemaFolding.Fold(topLevelObject);

            ArmJsonSchema[] resourceSchemas = ((ArmOneOfCombinator)((ArmListSchema)foldedSchema.Properties["resources"]).Items).OneOf;

            var dict = new Dictionary<string, PropertyTable>();
            foreach (ArmJsonSchema schema in resourceSchemas)
            {
                if (!(schema is ArmObjectSchema obj))
                {
                    continue;
                }

                string propertyFullName = (string)((ArmConcreteSchema)obj.Properties["type"]).Enum[0];
                string[] propertyNameElements = propertyFullName.Split(s_propertyNameSeparator);
                string propertyNamespace = propertyNameElements[0];
                string propertyName = propertyNameElements[1];

                if (!dict.TryGetValue(propertyNamespace, out PropertyTable table))
                {
                    table = new PropertyTable();
                    dict[propertyNamespace] = table;
                }

                bool required = obj.Required != null && obj.Required.Contains(propertyFullName);

                table[propertyName] = BuildPropertyEntry(propertyNamespace, propertyName, required, obj);
            }

            return dict;
        }

        public Property BuildPropertyEntry(string propertyNamespace, string propertyName, bool required, ArmJsonSchema schema)
        {
            if (schema is ArmObjectSchema obj)
            {
                return BuildPropertyEntry(propertyNamespace, propertyName, required, obj);
            }

            return new Property(propertyName, schema.Type?[0], required);
        }

        public ObjectProperty BuildPropertyEntry(string propertyNamespace, string propertyName, bool required, ArmObjectSchema obj)
        {
            var table = new PropertyTable();

            if (obj.Properties.TryGetValue("properties", out ArmJsonSchema propertiesSchema))
            {
                foreach (KeyValuePair<string, ArmJsonSchema> entry in ((ArmObjectSchema)propertiesSchema).Properties)
                {
                    bool propertyRequired = obj.Required != null && obj.Required.Contains(entry.Key);
                    table[entry.Key] = BuildPropertyEntry(propertyNamespace, entry.Key, propertyRequired, entry.Value);
                }
            }

            return new ObjectProperty(propertyName, required)
            {
                Body = table,
            };
        }
    }
}

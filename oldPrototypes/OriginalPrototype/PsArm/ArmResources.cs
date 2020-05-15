
// Copyright (c) Microsoft Corporation.
// All rights reserved.

using System.Collections;
using System.Collections.Generic;

namespace PsArm
{
    public class ArmResourceNamespace : IEnumerable<ArmResourceBuilder>
    {
        private List<ArmResourceBuilder> _resources;

        public ArmResourceNamespace(string name)
        {
            Name = name;
            _resources = new List<ArmResourceBuilder>();
        }

        public string Name { get; }

        public void AddResource(ArmResourceBuilder resource)
        {
            resource.ApplyNamespace(Name);
            _resources.Add(resource);
        }

        public IEnumerator<ArmResourceBuilder> GetEnumerator()
        {
            return _resources.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ArmResourceBuilder
    {
        public ArmResourceBuilder(ArmValue name, string type, ArmValue apiVersion)
        {
            Name = name;
            Type = type;
            DependsOn = new List<ArmValue>();
            Properties = new ArmPropertyBuilder();
        }

        public ArmValue ApiVersion { get; }

        public ArmValue Name { get; }

        public string Type { private set; get; }

        public ArmValue Location { get; set; }

        public List<ArmValue> DependsOn { get; }

        public ArmPropertyBuilder Properties { get; }

        public ArmValue Kind { get; set; }

        public ArmSku Sku { get; set; }

        public ArmObjectValue ToArmValue()
        {
            var dict = new Dictionary<string, ArmValue>();

            dict["name"] = Name;
            dict["type"] = new ArmStringValue(Type);

            if (Location != null)
            {
                dict["location"] = Location;
            }

            if (DependsOn.Count > 0)
            {
                dict["dependsOn"] = new ArmArrayValue(DependsOn);
            }

            if (Kind != null)
            {
                dict["kind"] = Kind;
            }

            if (Sku != null)
            {
                dict["sku"] = Sku.ToArmValue();
            }

            if (Properties.HasProperties)
            {
                dict["properties"] = Properties.ToArmValue();
            }

            return new ArmObjectValue(dict);
        }

        public void ApplyNamespace(string nspace)
        {
            Type = nspace + "/" + Type;
        }
    }

    public class ArmSku
    {
        public ArmSku(
            ArmValue name,
            ArmValue tier,
            ArmValue size,
            ArmValue family,
            ArmValue capacity)
        {
            Name = name;
            Tier = tier;
            Size = size;
            Family = family;
            Capacity = capacity;
        }

        public ArmSku(Hashtable hashtable)
        {
            object item = hashtable["name"];
            if (item != null)
            {
                Name = ArmValueTypeConverter.Create(item);
            }

            item = hashtable["tier"];
            if (item != null)
            {
                Tier = ArmValueTypeConverter.Create(item);
            }

            item = hashtable["size"];
            if (item != null)
            {
                Tier = ArmValueTypeConverter.Create(item);
            }

            item = hashtable["family"];
            if (item != null)
            {
                Family = ArmValueTypeConverter.Create(item);
            }

            item = hashtable["capacity"];
            if (item != null)
            {
                Capacity = ArmValueTypeConverter.Create(item);
            }
        }

        public ArmValue Name { get; }

        public ArmValue Tier { get; }

        public ArmValue Size { get; }

        public ArmValue Family { get; }

        public ArmValue Capacity { get; }

        public ArmValue ToArmValue()
        {
            var dict = new Dictionary<string, ArmValue>();

            if (Name != null) { dict["name"] = Name; }
            if (Tier != null) { dict["tier"] = Tier; }
            if (Size != null) { dict["size"] = Size; }
            if (Family != null) { dict["family"] = Family; }
            if (Capacity != null) { dict["capacity"] = Capacity; }

            return new ArmObjectValue(dict);
        }
    }

    public interface IArmProperty
    {
        string Name { get; }

        ArmValue ToArmValue();
    }

    public class ArmSimpleProperty : IArmProperty
    {
        public ArmSimpleProperty(string name, ArmValue value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public ArmValue Value { get; }

        public ArmValue ToArmValue()
        {
            return Value;
        }
    }

    public class ArmArrayProperty : IArmProperty
    {
        private readonly ArmValue _value;

        public ArmArrayProperty(ArmValue value)
        {
            _value = value;
        }

        public string Name { get; }

        public ArmValue ToArmValue()
        {
            return _value;
        }
    }

    public class ArmObjectProperty : IArmProperty
    {
        public ArmObjectProperty(string name)
        {
            Name = name;
            SubProperties = new ArmPropertyBuilder();
        }

        public string Name { get; }

        public ArmPropertyBuilder SubProperties { get; }

        public ArmValue ToArmValue()
        {
            return SubProperties.ToArmValue();
        }
    }

    public class ArmPropertyBuilder
    {
        private Dictionary<string, IArmProperty> _fieldProperties;

        private Dictionary<string, List<ArmArrayProperty>> _arrayProperties;

        public ArmPropertyBuilder()
        {
            _fieldProperties = new Dictionary<string, IArmProperty>();
            _arrayProperties = new Dictionary<string, List<ArmArrayProperty>>();
        }

        public void AddProperty(ArmObjectProperty property)
        {
            _fieldProperties.Add(property.Name, property);
        }

        public void AddProperty(ArmSimpleProperty property)
        {
            _fieldProperties.Add(property.Name, property);
        }

        public void AddProperty(ArmArrayProperty property)
        {
            if (!_arrayProperties.TryGetValue(property.Name, out List<ArmArrayProperty> propertyList))
            {
                propertyList = new List<ArmArrayProperty>();
                _arrayProperties[property.Name] = propertyList;
            }

            propertyList.Add(property);
        }

        public bool HasProperties
        {
            get
            {
                return _arrayProperties.Count > 0
                    || _fieldProperties.Count > 0;
            }
        }

        public ArmValue ToArmValue()
        {
            var dict = new Dictionary<string, ArmValue>();

            foreach (KeyValuePair<string, IArmProperty> objectProperty in _fieldProperties)
            {
                dict.Add(objectProperty.Key, objectProperty.Value.ToArmValue());
            }

            foreach (KeyValuePair<string, List<ArmArrayProperty>> arrayProperty in _arrayProperties)
            {
                var propertyValues = new List<ArmValue>();
                foreach (ArmArrayProperty property in arrayProperty.Value)
                {
                    propertyValues.Add(property.ToArmValue());
                }
                dict.Add(arrayProperty.Key, new ArmArrayValue(propertyValues));
            }

            return new ArmObjectValue(dict);
        }
    }
}
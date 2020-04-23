using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PSArm
{
    public abstract class ArmPropertyInstance
    {
        public ArmPropertyInstance(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        public abstract JToken ToJson();

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }

    public class ArmPropertyValue : ArmPropertyInstance
    {
        public ArmPropertyValue(string propertyName, object value)
            : base(propertyName)
        {
            Value = value;
        }

        public object Value { get; }

        public override JToken ToJson()
        {
            return new JValue(Value);
        }
    }

    public class ArmPropertyArrayItem : ArmPropertyInstance
    {
        public ArmPropertyArrayItem(string propertyName, ArmPropertyInstance item)
            : base(propertyName)
        {
            Item = item;
        }

        public ArmPropertyInstance Item { get; }

        public override JToken ToJson()
        {
            return Item.ToJson();
        }
    }

    public class ArmPropertyArray : ArmPropertyInstance
    {
        internal ArmPropertyArray(string propertyName)
            : base(propertyName)
        {
            Items = new List<ArmPropertyInstance>();
        }

        public List<ArmPropertyInstance> Items { get; }

        public override JToken ToJson()
        {
            var jArr = new JArray();
            foreach (ArmPropertyInstance item in Items)
            {
                jArr.Add(item);
            }
            return jArr;
        }
    }

    public class ArmPropertyObject : ArmPropertyInstance
    {
        public ArmPropertyObject(string propertyName)
            : this(propertyName, new Dictionary<string, ArmPropertyInstance>())
        {
        }

        internal ArmPropertyObject(string propertyName, Dictionary<string, ArmPropertyInstance> fields)
            : base(propertyName)
        {
            Fields = fields;
        }

        public Dictionary<string, ArmPropertyInstance> Fields { get; }

        public override JToken ToJson()
        {
            var json = new JObject();
            foreach (KeyValuePair<string, ArmPropertyInstance> field in Fields)
            {
                json[field.Key] = field.Value.ToJson();
            }
            return json;
        }
    }

    public class ArmPropertyObjectBuilder
    {
        private readonly Dictionary<string, ArmPropertyInstance> _fields;

        private readonly string _propertyName;

        public ArmPropertyObjectBuilder(string propertyName)
        {
            _propertyName = propertyName;
            _fields = new Dictionary<string, ArmPropertyInstance>();
        }

        public void Add(ArmPropertyInstance propertyInstance)
        {
            switch (propertyInstance)
            {
                case ArmPropertyValue value:
                    Add(value);
                    return;

                default:
                    _fields[propertyInstance.PropertyName] = propertyInstance;
                    return;
            }
        }

        public void Add(ArmPropertyArrayItem arrayItem)
        {
            if (!_fields.TryGetValue(arrayItem.PropertyName, out ArmPropertyInstance propertyInstance))
            {
                propertyInstance = new ArmPropertyArray(arrayItem.PropertyName);
                _fields[arrayItem.PropertyName] = propertyInstance;
            }

            ((ArmPropertyArray)propertyInstance).Items.Add(arrayItem.Item);
        }

        public ArmPropertyObject GetObject()
        {
            return new ArmPropertyObject(_propertyName, _fields);
        }
    }
}
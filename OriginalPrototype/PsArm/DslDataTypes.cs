using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management.Automation;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace PsArm
{

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ArmType
    {
        [EnumMember(Value = "string")]
        String,

        [EnumMember(Value = "securestring")]
        SecureString,

        [EnumMember(Value = "int")]
        Int,

        [EnumMember(Value = "bool")]
        Bool,

        [EnumMember(Value = "object")]
        Object,

        [EnumMember(Value = "secureObject")]
        SecureObject,

        [EnumMember(Value = "array")]
        Array
    }

    internal static class ArmTypeExtensions
    {
        public static string ToTypeName(this ArmType type)
        {
            switch (type)
            {
                case ArmType.Array:
                    return "array";

                case ArmType.Bool:
                    return "bool";

                case ArmType.Int:
                    return "int";

                case ArmType.Object:
                    return "object";

                case ArmType.SecureObject:
                    return "secureObject";

                case ArmType.SecureString:
                    return "securestring";

                case ArmType.String:
                    return "string";

                default:
                    throw new InvalidOperationException($"Unknown ARM type value: {type}");
            }
        }

        public static JToken ToJson(this ArmType type)
        {
            return new JValue(type.ToTypeName());
        }

        public static ArmStringValue ToArmValue(this ArmType type)
        {
            return new ArmStringValue(type.ToTypeName());
        }
    }

    [TypeConverter(typeof(ArmValueTypeConverter))]
    public abstract class ArmValue
    {
        public abstract JToken ToJson();

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }

    public abstract class ArmPrimitiveValue<TVal> : ArmValue
    {
        public ArmPrimitiveValue(TVal value)
        {
            Value = value;
        }

        public TVal Value { get; }

        public override JToken ToJson()
        {
            return new JValue(Value);
        }
    }

    [TypeConverter(typeof(ArmValueTypeConverter))]
    public class ArmStringValue : ArmPrimitiveValue<string>
    {
        public ArmStringValue(string value) : base(value)
        {
        }
    }

    [TypeConverter(typeof(ArmValueTypeConverter))]
    public class ArmNumberValue : ArmPrimitiveValue<decimal>
    {
        public ArmNumberValue(decimal value) : base(value)
        {
        }
    }

    [TypeConverter(typeof(ArmValueTypeConverter))]
    public class ArmBoolValue : ArmPrimitiveValue<bool>
    {
        public ArmBoolValue(bool value) : base(value)
        {
        }
    }

    [TypeConverter(typeof(ArmValueTypeConverter))]
    public class ArmNullValue : ArmPrimitiveValue<object>
    {
        public ArmNullValue() : base(null)
        {
        }
    }

    [TypeConverter(typeof(ArmValueTypeConverter))]
    public class ArmObjectValue : ArmValue, IReadOnlyDictionary<string, ArmValue>
    {
        private readonly IReadOnlyDictionary<string, ArmValue> _dict;

        public ArmObjectValue(IReadOnlyDictionary<string, ArmValue> values)
        {
            var dict = new Dictionary<string, ArmValue>();
            foreach (KeyValuePair<string, ArmValue> value in values)
            {
                dict.Add(value.Key, value.Value);
            }
            _dict = dict;
        }

        public override JToken ToJson()
        {
            var obj = new JObject();
            foreach (KeyValuePair<string, ArmValue> value in _dict)
            {
                obj[value.Key] = value.Value.ToJson();
            }
            return obj;
        }

        public ArmValue this[string key] => _dict[key];

        public IEnumerable<string> Keys => _dict.Keys;

        public IEnumerable<ArmValue> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, ArmValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }


        public bool TryGetValue(string key, out ArmValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [TypeConverter(typeof(ArmValueTypeConverter))]
    public class ArmArrayValue : ArmValue, IReadOnlyList<ArmValue>
    {
        private readonly IReadOnlyList<ArmValue> _values;

        public ArmArrayValue(IReadOnlyCollection<ArmValue> values)
        {
            _values = new List<ArmValue>(values);
        }

        public override JToken ToJson()
        {
            var arr = new JArray();
            foreach (ArmValue item in this)
            {
                arr.Add(item.ToJson());
            }
            return arr;
        }


        public ArmValue this[int index] => _values[index];

        public int Count => _values.Count;

        public IEnumerator<ArmValue> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ArmVariable : ArmExpression
    {
        public ArmVariable(ArmValue value)
        {
            Value = value;
        }

        public string Name { get; set; }

        public ArmValue Value { get; }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            return new StringBuilder("variables(\'")
                .Append(Name)
                .Append("\')");
        }
    }

    public class ArmVariableTable : ArmValue, IDictionary<string, ArmVariable>
    {
        private readonly IDictionary<string, ArmVariable> _dict;

        public ArmVariableTable()
        {
            _dict = new Dictionary<string, ArmVariable>();
        }

        public override JToken ToJson()
        {
            var obj = new JObject();
            foreach (KeyValuePair<string, ArmVariable> variable in this)
            {
                obj[variable.Key] = variable.Value.Value.ToJson();
            }
            return obj;
        }

        public ArmVariable this[string key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<ArmVariable> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => _dict.IsReadOnly;

        public void Add(string key, ArmVariable value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, ArmVariable> item)
        {
            _dict.Add(item);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, ArmVariable> item)
        {
            return _dict.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, ArmVariable>[] array, int arrayIndex)
        {
            _dict.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, ArmVariable>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<string, ArmVariable> item)
        {
            return _dict.Remove(item);
        }

        public bool TryGetValue(string key, out ArmVariable value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public abstract class ArmParameter : ArmExpression
    {
        protected ArmParameter(
            ArmType type,
            ArmValue defaultValue,
            IReadOnlyCollection<ArmValue> allowedValues,
            IReadOnlyDictionary<string, ArmValue> metadata)
        {
            Type = type;
            DefaultValue = defaultValue;
            AllowedValues = allowedValues;
            Metadata = metadata;
        }

        public string Name { get; set; }

        public ArmType Type { get; }

        public ArmValue DefaultValue { get; }

        public IReadOnlyCollection<ArmValue> AllowedValues { get; }

        public IReadOnlyDictionary<string, ArmValue> Metadata { get; }

        public ArmValue ToArmValue()
        {
            return new ArmObjectValue(CreateArmObject());
        }

        public override string ToString()
        {
            return ToArmValue().ToString();
        }

        protected virtual Dictionary<string, ArmValue> CreateArmObject()
        {
            var obj = new Dictionary<string, ArmValue>();
            
            obj["type"] = Type.ToArmValue();

            if (DefaultValue != null)
            {
                obj["defaultValue"] = DefaultValue;
            }

            if (AllowedValues != null)
            {
                obj["allowedValues"] = new ArmArrayValue(AllowedValues);
            }

            if (Metadata != null)
            {
                obj["metadata"] = new ArmObjectValue(Metadata);
            }

            return obj;
        }

        internal override StringBuilder ToInnerExpressionSyntax()
        {
            return new StringBuilder("parameters(\'")
                .Append(Name)
                .Append("\')");
        }
    }

    public class ArmStringParameter : ArmParameter
    {
        public ArmStringParameter(
            ArmValue defaultValue,
            IReadOnlyCollection<ArmValue> allowedValues,
            IReadOnlyDictionary<string, ArmValue> metadata,
            bool isSecure,
            int minLength,
            int maxLength)
            : base(isSecure ? ArmType.SecureString : ArmType.String, defaultValue, allowedValues, metadata)
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public int MinLength { get; }

        public int MaxLength { get; }

        protected override Dictionary<string, ArmValue> CreateArmObject()
        {
            var obj = base.CreateArmObject();

            if (MinLength >= 0)
            {
                obj["minLength"] = new ArmNumberValue(MinLength);
            }

            if (MaxLength >= 0)
            {
                obj["maxLength"] = new ArmNumberValue(MaxLength);
            }

            return obj;
        }
    }

    public class ArmIntParameter : ArmParameter
    {
        public ArmIntParameter(
            ArmValue defaultValue,
            IReadOnlyCollection<ArmValue> allowedValues,
            IReadOnlyDictionary<string, ArmValue> metadata,
            int minValue,
            int maxValue)
                : base(ArmType.Int, defaultValue, allowedValues, metadata)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public int MinValue { get; }

        public int MaxValue { get; }

        protected override Dictionary<string, ArmValue> CreateArmObject()
        {
            var obj = base.CreateArmObject();

            if (MinValue >= 0)
            {
                obj["minValue"] = new ArmNumberValue(MinValue);
            }

            if (MaxValue >= 0)
            {
                obj["maxValue"] = new ArmNumberValue(MaxValue);
            }

            return obj;
        }
    }

    public class ArmBoolParameter : ArmParameter
    {
        public ArmBoolParameter(
            ArmValue defaultValue,
            IReadOnlyCollection<ArmValue> allowedValues,
            IReadOnlyDictionary<string, ArmValue> metadata)
                : base(ArmType.Bool, defaultValue, allowedValues, metadata)
        {
        }
    }

    public class ArmObjectParameter : ArmParameter
    {
        public ArmObjectParameter(
            bool isSecure,
            ArmValue defaultValue,
            IReadOnlyCollection<ArmValue> allowedValues,
            IReadOnlyDictionary<string, ArmValue> metadata)
                : base(isSecure ? ArmType.SecureObject : ArmType.Object, defaultValue, allowedValues, metadata)
        {
        }
    }

    public class ArmArrayParameter : ArmParameter
    {
        public ArmArrayParameter(
            ArmValue defaultValue,
            IReadOnlyCollection<ArmValue> allowedValues,
            IReadOnlyDictionary<string, ArmValue> metadata,
            int minLength,
            int maxLength)
                : base(ArmType.Array, defaultValue, allowedValues, metadata)
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public int MinLength { get; }

        public int MaxLength { get; }

        protected override Dictionary<string, ArmValue> CreateArmObject()
        {
            var obj = base.CreateArmObject();

            if (MinLength >= 0)
            {
                obj["minLength"] = new ArmNumberValue(MinLength);
            }

            if (MaxLength >= 0)
            {
                obj["maxLength"] = new ArmNumberValue(MaxLength);
            }

            return obj;
        }
    }

    public class ArmParameterTable : ArmValue, IDictionary<string, ArmParameter>
    {
        private readonly Dictionary<string, ArmParameter> _dict;

        public ArmParameterTable()
        {
            _dict = new Dictionary<string, ArmParameter>();
        }

        public ArmParameter this[string key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<ArmParameter> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => true;

        public void Add(string key, ArmParameter value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, ArmParameter> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, ArmParameter> item)
        {
            return _dict.TryGetValue(item.Key, out ArmParameter value)
                && value == item.Value;
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, ArmParameter>[] array, int arrayIndex)
        {
            ((IDictionary<string, ArmParameter>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, ArmParameter>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<string, ArmParameter> item)
        {
            return ((IDictionary<string, ArmParameter>)_dict).Remove(item);
        }

        public override JToken ToJson()
        {
            var obj = new JObject();
            foreach (KeyValuePair<string, ArmParameter> item in this)
            {
                obj[item.Key] = item.Value.ToArmValue().ToJson();
            }
            return obj;
        }

        public bool TryGetValue(string key, out ArmParameter value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class ArmValueTypeConverter : PSTypeConverter
    {
        public static ArmValue Create(object obj)
        {
            if (obj == null)
            {
                return new ArmNullValue();
            }

            switch (obj)
            {
                case ArmValue value:
                    return value;

                case ArmExpressionBuilder expr:
                    return expr.GetArmExpression();

                case decimal d:
                    return new ArmNumberValue(d);

                case string s:
                    return new ArmStringValue(s);

                case bool b:
                    return new ArmBoolValue(b);

                case Hashtable htbl:
                    var dict = new Dictionary<string, ArmValue>();
                    foreach (DictionaryEntry item in htbl)
                    {
                        if (item.Key.GetType() != typeof(string))
                        {
                            throw new Exception($"Hashtable key must be a string. Got {item.Key}");
                        }

                        dict[(string)item.Key] = Create(item.Value);
                    }
                    return new ArmObjectValue(dict);

                case IEnumerable enumerable:
                    var arrVals = new List<ArmValue>();
                    foreach (object arrVal in enumerable)
                    {
                        arrVals.Add(Create(arrVal));
                    }
                    return new ArmArrayValue(arrVals);
            }

            throw new Exception($"Value {obj} cannot be converted to ArmValue");
        }

        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (!typeof(ArmValue).IsAssignableFrom(destinationType))
            {
                return false;
            }

            if (sourceValue == null)
            {
                return true;
            }

            return sourceValue == null
                || sourceValue is string
                || sourceValue is decimal
                || sourceValue is bool
                || sourceValue is ArmValue
                || sourceValue is ArmExpressionBuilder
                || sourceValue is Hashtable
                || sourceValue is IEnumerable;
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return CanConvertFrom(sourceValue, destinationType);
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return Create(sourceValue);
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return Create(sourceValue);
        }
    }

    public class ArmTemplate
    {
        private const string SCHEMA = "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#";

        public ArmTemplate()
        {
            Parameters = new ArmParameterTable();
            Variables = new ArmVariableTable();
            Resources = new List<ArmResourceBuilder>();
        }

        public ArmParameterTable Parameters { get; }

        public ArmVariableTable Variables { get; }

        public List<ArmResourceBuilder> Resources { get; }

        public JToken ToJson()
        {
            var obj = new JObject();

            if (Parameters.Count > 0)
            {
                obj["parameters"] = Parameters.ToJson();
            }

            if (Variables.Count > 0)
            {
                obj["variables"] = Variables.ToJson();
            }

            var resourceArray = new JArray();
            foreach (ArmResourceBuilder resource in Resources)
            {
                resourceArray.Add(resource.ToArmValue().ToJson());
            }
            obj["resources"] = resourceArray;

            return obj;
        }
        
        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}
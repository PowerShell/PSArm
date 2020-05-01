using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PSArm
{
    internal static class ArmTypeConversion
    {
        public static IArmExpression Convert(object obj)
        {
            switch (obj)
            {
                case null:
                    return null;

                case IArmExpression expression:
                    return expression;

                case PSObject psObj:
                    return Convert(psObj.BaseObject);

                case string s:
                    return new ArmStringLiteral(s);

                case int i:
                    return new ArmIntLiteral(i);

                case bool b:
                    return new ArmBoolLiteral(b);

                default:
                    throw new ArgumentException($"Unable to covert value '{obj}' of type '{obj.GetType()}' to IArmExpression");
            }
        }
    }

    public class ArmTypeConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            switch (sourceValue)
            {
                case string _:
                    return destinationType.IsAssignableFrom(typeof(ArmStringLiteral));

                case int _:
                    return destinationType.IsAssignableFrom(typeof(ArmIntLiteral));

                case bool _:
                    return destinationType.IsAssignableFrom(typeof(ArmBoolLiteral));

                default:
                    return false;
            }
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return CanConvertFrom(sourceValue, destinationType);
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return ArmTypeConversion.Convert(sourceValue);
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return ConvertFrom(sourceValue, destinationType, formatProvider, ignoreCase);
        }
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public interface IArmExpression
    {
        string ToExpressionString();

        string ToInnerExpressionString();

        IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters);
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public abstract class ArmLiteral : IArmExpression
    {
        public IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters) => this;

        public abstract string ToExpressionString();

        public abstract string ToInnerExpressionString();

        public override string ToString() => ToExpressionString();

        public abstract object GetValue();
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public abstract class ArmLiteral<T> : ArmLiteral
    {
        public ArmLiteral(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public override object GetValue() => Value;
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmStringLiteral : ArmLiteral<string>
    {
        public ArmStringLiteral(string value) : base(value)
        {
        }

        public override string ToExpressionString()
        {
            return Value.StartsWith("[") && Value.EndsWith("]")
                ? "[" + Value
                : Value;
        }

        public override string ToInnerExpressionString()
        {
            return "'" + Value + "'";
        }
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmIntLiteral : ArmLiteral<int>
    {
        public ArmIntLiteral(int value) : base(value)
        {
        }

        public override string ToExpressionString() => Value.ToString();

        public override string ToInnerExpressionString() => ToExpressionString();
    }

    [TypeConverter(typeof(ArmTypeConverter))]
    public class ArmBoolLiteral : ArmLiteral<bool>
    {
        public ArmBoolLiteral(bool value) : base(value)
        {
        }

        public override string ToExpressionString()
        {
            return Value
                ? "true"
                : "false";
        }

        public override string ToInnerExpressionString() => ToExpressionString();
    }

    public abstract class ArmOperation : DynamicObject, IArmExpression
    {
        public string ToExpressionString()
        {
            return new StringBuilder()
                .Append('[')
                .Append(ToInnerExpressionString())
                .Append(']')
                .ToString();
        }

        public abstract string ToInnerExpressionString();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new ArmMemberAccess(this, UnPascal(binder.Name));
            return true;
        }

        public override string ToString() => ToExpressionString();

        public abstract IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters);

        private string UnPascal(string s)
        {
            return char.IsUpper(s[0])
                ? char.ToLower(s[0]) + s.Substring(1)
                : s;
        }
    }

    public class ArmFunctionCall : ArmOperation
    {
        public ArmFunctionCall(string functionName, IArmExpression[] arguments)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }

        public string FunctionName { get; }

        public IArmExpression[] Arguments { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            if (Arguments == null)
            {
                return this;
            }

            var args = new List<IArmExpression>();
            foreach (IArmExpression arg in Arguments)
            {
                args.Add(arg.Instantiate(parameters));
            }

            return new ArmFunctionCall(FunctionName, args.ToArray());
        }

        public override string ToInnerExpressionString()
        {
            var sb = new StringBuilder()
                .Append(FunctionName)
                .Append('(');

            if (Arguments != null && Arguments.Length > 0)
            {
                sb.Append(Arguments[0].ToInnerExpressionString());
                for (int i = 1; i < Arguments.Length; i++)
                {
                    sb.Append(", ")
                        .Append(Arguments[i].ToInnerExpressionString());
                }
            }

            sb.Append(')');
            return sb.ToString();
        }
    }

    public class ArmMemberAccess : ArmOperation
    {
        public ArmMemberAccess(ArmOperation expression, string member)
        {
            Expression = expression;
            Member = member;
        }

        public ArmOperation Expression { get; }

        public string Member { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmMemberAccess((ArmOperation)Expression.Instantiate(parameters), Member);
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append(Expression.ToInnerExpressionString())
                .Append('.')
                .Append(Member)
                .ToString();
        }
    }

    public class ArmIndexAccess : ArmOperation
    {
        public ArmIndexAccess(ArmOperation expression, int index)
        {
            Expression = expression;
            Index = index;
        }

        public ArmOperation Expression { get; }

        public int Index { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmIndexAccess((ArmOperation)Expression.Instantiate(parameters), Index);
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append(Expression.ToInnerExpressionString())
                .Append('[')
                .Append(Index)
                .Append(']')
                .ToString();
        }
    }

    public class ArmConcatCall : ArmFunctionCall
    {
        public ArmConcatCall(IArmExpression[] arguments)
            : base("concat", arguments)
        {
        }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            var args = new List<IArmExpression>(Arguments.Length);
            bool canFlatten = true;
            foreach (IArmExpression arg in Arguments)
            {
                IArmExpression resolved = arg.Instantiate(parameters);

                if (!(resolved is ArmStringLiteral))
                {
                    canFlatten = false;
                }

                args.Add(resolved);
            }

            if (canFlatten)
            {
                var sb = new StringBuilder();
                foreach (ArmStringLiteral strArg in args)
                {
                    sb.Append(strArg.Value);
                }
                return new ArmStringLiteral(sb.ToString());
            }

            return new ArmConcatCall(args.ToArray());
        }
    }

    public class ArmParameter : ArmOperation
    {
        public ArmParameter(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Type { get; set; }

        public object[] AllowedValues { get; set; }

        public object DefaultValue { get; set; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            ArmLiteral value = parameters[Name];

            if (AllowedValues != null)
            {
                bool found = false;
                foreach (object allowedValue in AllowedValues)
                {
                    if (object.Equals(value.GetValue(), allowedValue))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"Parameter '{Name}' does not have '{value.GetValue()}' as an allowed value");
                }
            }

            return value;
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append("parameters('")
                .Append(Name)
                .Append("')")
                .ToString();
        }

        public JObject ToJson()
        {
            var jObj = new JObject();

            if (Type != null)
            {
                jObj["type"] = Type;
            }

            if (AllowedValues != null)
            {
                var jArr = new JArray();
                foreach (object val in AllowedValues)
                {
                    jArr.Add(val);
                }
                jObj["allowedValues"] = jArr;
            }

            if (DefaultValue != null)
            {
                jObj["defaultValue"] = new JValue(DefaultValue);
            }

            return jObj;
        }
    }

    public class ArmVariable : ArmOperation
    {
        public ArmVariable(string name, IArmExpression value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public IArmExpression Value { get; }

        public override IArmExpression Instantiate(IReadOnlyDictionary<string, ArmLiteral> parameters)
        {
            return new ArmVariable(Name, Value.Instantiate(parameters));
        }

        public override string ToInnerExpressionString()
        {
            return new StringBuilder()
                .Append("variables('")
                .Append(Name)
                .Append("')")
                .ToString();
        }

        public JValue ToJson()
        {
            return new JValue(Value.ToExpressionString());
        }
    }

    public abstract class ArmBuiltinCommand : Cmdlet
    {
        protected ArmBuiltinCommand(string function)
        {
            Function = function;
        }

        public string Function { get; }

        protected virtual IArmExpression[] GetArguments()
        {
            return null;
        }

        protected override void EndProcessing()
        {
            WriteObject(new ArmFunctionCall(Function, GetArguments()));
        }
    }

    [Alias("Concat")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinConcat")]
    public class ArmBuiltinConcatCommand : ArmBuiltinCommand
    {
        public ArmBuiltinConcatCommand() : base("concat")
        {
        }

        [ValidateNotNullOrEmpty]
        [Parameter(ValueFromRemainingArguments = true)]
        public IArmExpression[] Arguments { get; set; }

        protected override void EndProcessing()
        {
            WriteObject(new ArmConcatCall(Arguments));
        }
    }

    [Alias("ResourceId")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinResourceId")]
    public class ArmBuiltinResourceIdCommand : ArmBuiltinCommand
    {
        public ArmBuiltinResourceIdCommand() : base("resourceId")
        {
        }

        [ValidateNotNull]
        [Parameter]
        public IArmExpression SubscriptionId { get; set; }

        [ValidateNotNull]
        [Parameter]
        public IArmExpression ResourceGroupName { get; set; }

        [ValidateNotNull]
        [Parameter(Position = 0, Mandatory = true)]
        public IArmExpression ResourceType { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 1, Mandatory = true, ValueFromRemainingArguments = true)]
        public IArmExpression[] ResourceName { get; set; }

        protected override IArmExpression[] GetArguments()
        {
            var args = new List<IArmExpression>();

            if (SubscriptionId != null)
            {
                args.Add(SubscriptionId);
            }

            if (ResourceGroupName != null)
            {
                args.Add(ResourceGroupName);
            }

            args.Add(ResourceType);
            args.AddRange(ResourceName);

            return args.ToArray();
        }
    }

    [Alias("ResourceGroup")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinResourceGroup")]
    public class InvokeArmBuiltinResourceGroupCommand : ArmBuiltinCommand
    {
        public InvokeArmBuiltinResourceGroupCommand() : base("resourceGroup")
        {
        }
    }

    [Alias("UniqueString")]
    [Cmdlet(VerbsLifecycle.Invoke, "ArmBuiltinUniqueString")]
    public class InvokeArmBuiltinUniqueString : ArmBuiltinCommand
    {
        public InvokeArmBuiltinUniqueString() : base("uniqueString")
        {
        }

        [Parameter(Mandatory = true, Position = 0, ValueFromRemainingArguments = true)]
        public IArmExpression[] Input { get; set; }

        protected override IArmExpression[] GetArguments()
        {
            return Input;
        }
    }
}
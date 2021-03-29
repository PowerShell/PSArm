
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSArm.Internal;
using PSArm.Templates.Operations;
using PSArm.Templates.Primitives;
using System;
using System.Collections.Generic;

namespace PSArm.Serialization
{
    internal struct ArmExpressionParser
    {
        private const string ParametersFunction = "parameters";

        private const string VariablesFunction = "variables";

        public IArmString ParseExpression(string s)
        {
            if (s.Length == 0)
            {
                return new ArmStringLiteral(string.Empty);
            }

            if (s.Length == 1              // All strings of length 1 are literals
                || s[0] != '['             // ARM expressions start with '['
                || s[s.Length - 1] != ']') // ARM expressions end with ']'
            {
                return new ArmStringLiteral(s);
            }

            // Starting with '[[' indicates an escaped literal
            if (s[1] == '[')
            {
                return new ArmStringLiteral(s.Substring(1));
            }

            return ParseWithTokenizer(s);
        }

        private IArmString ParseWithTokenizer(string s)
        {
            var tokenizer = new ArmExpressionTokenizer(s);

            return ParseFullExpression(ref tokenizer);
        }

        private IArmString ParseFullExpression(ref ArmExpressionTokenizer tokenizer)
        {
            Expect(ref tokenizer, ArmExpressionTokenType.OpenBracket);
            IArmString wholeExpression = ParseInnerExpression(ref tokenizer, noLiteral: true);
            Expect(ref tokenizer, ArmExpressionTokenType.CloseBracket);
            Expect(ref tokenizer, ArmExpressionTokenType.EOF);
            return wholeExpression;
        }

        private IArmString ParseInnerExpression(ref ArmExpressionTokenizer tokenizer, bool noLiteral = false)
        {
            ArmExpressionToken token = tokenizer.NextToken();
            switch (token.Type)
            {
                case ArmExpressionTokenType.String:
                    if (noLiteral)
                    {
                        throw Error($"Expected a literal token but found a string in '{tokenizer.Input}' at index {tokenizer.PreviousIndex}");
                    }
                    return new ArmStringLiteral(token.CoerceToString());

                case ArmExpressionTokenType.Identifier:
                    return ParseIdentifierExpression(ref tokenizer, (ArmExpressionIdentifierToken)token);

                default:
                    throw Error($"Expected an expression token but found '{token}' in '{tokenizer.Input}' at index {tokenizer.PreviousIndex}");
            }
        }

        private ArmOperation ParseIdentifierExpression(ref ArmExpressionTokenizer tokenizer, ArmExpressionIdentifierToken identifier)
        {
            ArmExpressionToken token = tokenizer.NextToken();

            switch (token.Type)
            {
                case ArmExpressionTokenType.OpenParen:
                    ArmOperation expression = ParseCallExpression(ref tokenizer, identifier);
                    return ParseDotExpression(ref tokenizer, expression);

                default:
                    throw Error($"Expected a token of type '{ArmExpressionTokenType.OpenParen}' but instead got '{token}' in '{tokenizer.Input}' at index '{tokenizer.PreviousIndex}'");
            }
        }

        private ArmOperation ParseCallExpression(ref ArmExpressionTokenizer tokenizer, ArmExpressionIdentifierToken identifier)
        {
            var arguments = new List<ArmExpression>();

            ArmExpressionToken token;
            bool sawCloseParen = false;
            while (!sawCloseParen)
            {
                token = tokenizer.NextToken();
                switch (token.Type)
                {
                    case ArmExpressionTokenType.Identifier:
                        arguments.Add(ParseIdentifierExpression(ref tokenizer, (ArmExpressionIdentifierToken)token));
                        break;

                    case ArmExpressionTokenType.String:
                        arguments.Add(new ArmStringLiteral(token.CoerceToString()));
                        break;

                    case ArmExpressionTokenType.Integer:
                        arguments.Add(new ArmIntegerLiteral(token.CoerceToLong()));
                        break;

                    case ArmExpressionTokenType.Boolean:
                        arguments.Add(ArmBooleanLiteral.FromBool(token.CoerceToBool()));
                        break;

                    case ArmExpressionTokenType.CloseParen:
                        if (arguments.Count == 0)
                        {
                            sawCloseParen = true;
                            continue;
                        }
                        goto default;

                    default:
                        throw Error($"Expected an expression but instead got '{token}' in input '{tokenizer.Input}' at index {tokenizer.PreviousIndex}");
                }

                token = tokenizer.NextToken();
                switch (token.Type)
                {
                    case ArmExpressionTokenType.Comma:
                        break;

                    case ArmExpressionTokenType.CloseParen:
                        sawCloseParen = true;
                        break;

                    default:
                        throw Error($"Expected an expression delimiter but instead got '{token}' in input '{tokenizer.Input}' at index {tokenizer.PreviousIndex}");
                }
            }

            if (identifier.Identifier.Is(ParametersFunction))
            {
                return new ArmParameterReferenceExpression((IArmString)arguments[0]);
            }

            if (identifier.Identifier.Is(VariablesFunction))
            {
                return new ArmVariableReferenceExpression((IArmString)arguments[0]);
            }

            return new ArmFunctionCallExpression(new ArmStringLiteral(identifier.Identifier), arguments.ToArray());
        }

        private ArmOperation ParseDotExpression(ref ArmExpressionTokenizer tokenizer, ArmOperation lhs)
        {
            ArmExpressionToken token = tokenizer.NextToken();

            switch (token.Type)
            {
                case ArmExpressionTokenType.Dot:
                    token = tokenizer.NextToken();
                    switch (token.Type)
                    {
                        case ArmExpressionTokenType.Identifier:
                            return ParseDotExpression(ref tokenizer, new ArmMemberAccessExpression(lhs, new ArmStringLiteral(((ArmExpressionIdentifierToken)token).Identifier)));

                        default:
                            throw Error($"Expected token of type '{ArmExpressionTokenType.Identifier}' after '.' but instead got '{token}' at index {tokenizer.PreviousIndex} in input '{tokenizer.Input}'");
                    }

                default:
                    tokenizer.UngetToken(token);
                    return lhs;
            }
        }

        private ArmExpressionToken Expect(ref ArmExpressionTokenizer tokenizer, ArmExpressionTokenType expectedTokenType)
        {
            ArmExpressionToken token = tokenizer.NextToken();

            if (token.Type != expectedTokenType)
            {
                throw Error($"Expected token of type '{expectedTokenType}' but instead got '{token}' at index {tokenizer.PreviousIndex} in input '{tokenizer.Input}'");
            }

            return token;
        }

        private Exception Error(string message)
        {
            return new InvalidOperationException(message);
        }
    }

    internal struct ArmExpressionTokenizer
    {
        private static readonly char[] s_endOfIdentifierChars = new[]
        {
            '[',
            ']',
            '(',
            ')',
            ',',
            '\'',
        };

        private readonly string _expressionString;

        private int _oldIndex;

        private int _i;

        private bool _ended;

        public ArmExpressionTokenizer(string expressionString)
        {
            _expressionString = expressionString;
            _i = _oldIndex = 0;
            _ended = false;
        }

        public string Input => _expressionString;

        public int PreviousIndex => _oldIndex;

        public ArmExpressionToken NextToken()
        {
            SkipWhitespace();

            // Preserve the last index for debugging
            _oldIndex = _i;

            if (_i >= _expressionString.Length)
            {
                if (_ended)
                {
                    throw new InvalidOperationException($"Tokenizer has already reached end of input in expression '{_expressionString}'");
                }

                _ended = true;
                return new ArmExpressionSyntaxToken(ArmExpressionTokenType.EOF);
            }

            switch (_expressionString[_i])
            {
                case '[':
                    _i++;
                    return new ArmExpressionSyntaxToken(ArmExpressionTokenType.OpenBracket);

                case ']':
                    _i++;
                    return new ArmExpressionSyntaxToken(ArmExpressionTokenType.CloseBracket);

                case '(':
                    _i++;
                    return new ArmExpressionSyntaxToken(ArmExpressionTokenType.OpenParen);

                case ')':
                    _i++;
                    return new ArmExpressionSyntaxToken(ArmExpressionTokenType.CloseParen);

                case ',':
                    _i++;
                    return new ArmExpressionSyntaxToken(ArmExpressionTokenType.Comma);

                case '.':
                    _i++;
                    return new ArmExpressionSyntaxToken(ArmExpressionTokenType.Dot);

                case '\'':
                    return TokenizeString();

                default:
                    return TokenizeIdentifier();
            }
        }

        public void UngetToken(ArmExpressionToken token)
        {
            _i = _oldIndex;
        }

        private ArmExpressionIdentifierToken TokenizeIdentifier()
        {
            int end = _expressionString.IndexOfAny(s_endOfIdentifierChars, _i);

            if (end < 0)
            {
                return new ArmExpressionIdentifierToken(_expressionString.Substring(_i));
            }

            int start = _i;
            _i = end;
            return new ArmExpressionIdentifierToken(_expressionString.Substring(start, end - start));
        }

        private ArmExpressionStringToken TokenizeString()
        {
            _i++;

            if (_i >= _expressionString.Length)
            {
                throw new InvalidOperationException($"Hit unexpected end of input while parsing string in '{_expressionString}' at index {_i - 1}");
            }

            int end = _expressionString.IndexOf('\'', _i);

            if (end < 0)
            {
                throw new InvalidOperationException($"Hit unexpected end of input while parsing string in '{_expressionString}' at index {_i}");
            }

            int start = _i;
            _i = end + 1;
            return new ArmExpressionStringToken(_expressionString.Substring(start, end - start));
        }

        private void SkipWhitespace()
        {
            while (_i < _expressionString.Length && char.IsWhiteSpace(_expressionString[_i]))
            {
                _i++;
            }
        }
    }

    internal abstract class ArmExpressionToken
    {
        protected ArmExpressionToken(ArmExpressionTokenType type)
        {
            Type = type;
        }

        public ArmExpressionTokenType Type { get; }

        public override abstract string ToString();
    }

    internal class ArmExpressionSyntaxToken : ArmExpressionToken
    {
        public ArmExpressionSyntaxToken(ArmExpressionTokenType type)
            : base(type)
        {
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ArmExpressionTokenType.OpenBracket:
                    return "[";

                case ArmExpressionTokenType.CloseBracket:
                    return "]";

                case ArmExpressionTokenType.OpenParen:
                    return "(";

                case ArmExpressionTokenType.CloseParen:
                    return ")";

                case ArmExpressionTokenType.Comma:
                    return ",";

                case ArmExpressionTokenType.Dot:
                    return ".";

                case ArmExpressionTokenType.EOF:
                    return "<EOF>";

                default:
                    return $"<INVALID SYNTAX TOKEN VALUE OF TYPE '{Type}'>";
            }
        }
    }

    internal abstract class ArmExpressionValueLiteralToken<T> : ArmExpressionToken
    {
        protected ArmExpressionValueLiteralToken(T value, ArmExpressionTokenType type)
            : base(type)
        {
            Value = value;
        }

        public T Value { get; }
    }

    internal class ArmExpressionStringToken : ArmExpressionValueLiteralToken<string>
    {
        public ArmExpressionStringToken(string value)
            : base(value, ArmExpressionTokenType.String)
        {
        }

        public override string ToString()
        {
            return Value;
        }
    }

    internal class ArmExpressionIntegerToken : ArmExpressionValueLiteralToken<long>
    {
        public ArmExpressionIntegerToken(long value)
            : base(value, ArmExpressionTokenType.Integer)
        {
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class ArmExpressionBooleanToken : ArmExpressionValueLiteralToken<bool>
    {
        public ArmExpressionBooleanToken(bool value)
            : base(value, ArmExpressionTokenType.Boolean)
        {
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class ArmExpressionIdentifierToken : ArmExpressionToken
    {
        public ArmExpressionIdentifierToken(string identifier)
            : base(ArmExpressionTokenType.Identifier)
        {
            Identifier = identifier;
        }

        public string Identifier { get; }

        public override string ToString()
        {
            return Identifier;
        }
    }

    internal enum ArmExpressionTokenType
    {
        OpenBracket,
        CloseBracket,
        OpenParen,
        CloseParen,
        Comma,
        Dot,
        EOF,
        Identifier,
        String,
        Integer,
        Boolean,
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProgrammingLanguage
{
    public abstract class Token
    {
        public string Raw { get; set; }
        public uint Offset { get; set; }
        public uint Line { get; set; }
        public uint Column { get; set; }
    }

    public class StringLiteral : Token
    {
        public string Value { get; set; }
    }

    public class NumberLiteral : Token
    {
        public ulong Digits { get; set; }
        public byte DigitCount { get; set; }
        public int Exponent { get; set; }
        public bool Negated { get; set; }
        public bool IsInteger { get; set; } = true;
    }

    public class Identifier : Token
    {
    }

    public class Keyword : Token
    {
        public static readonly string[] Keywords =
        {
            "def", "if", "else", "while", "for", "return", "const", "var", "using", "event", "interface", "struct", "break", "namespace"
        };
    }

    public class Bracket : Token
    {
        public char Type { get; set; }
    }

    public class Operator : Token
    {
    }

    public class Whitespace : Token
    {
    }

    public class SyntaxError : Exception
    {
        public SyntaxError(string msg, uint line, uint column, uint offset) : base(msg)
        {
            Line = line;
            Column = column;
            Offset = offset;
        }

        public uint Line { get; private set; }
        public uint Column { get; private set; }
        public uint Offset { get; private set; }
    }

    public class Lexer
    {
        enum State
        {
            Root
        }

        readonly Stack<State> _states = new Stack<State>(new [] { State.Root });

        private PositionAwareReader _reader;

        public IEnumerable<Token> Tokenize(Stream stream)
        {
            _reader = new PositionAwareReader(stream);

            try
            {
                var state = _states.Peek();

                while (!_reader.EndOfStream)
                {
                    var nextChar = (char)_reader.Peek();
                    switch (state)
                    {
                        case State.Root:
                            if (nextChar == '"')
                            {
                                yield return LexString();
                            }
                            else if (char.IsWhiteSpace(nextChar))
                            {
                                yield return LexWhitespace();
                            }
                            else if (nextChar == '_' || char.IsLetter(nextChar))
                            {
                                yield return LexIdentifier();
                            }
                            else if (char.IsDigit(nextChar))
                            {
                                yield return LexNumber();
                            }
                            else if (nextChar == '(' || nextChar == ')' 
                                || nextChar == '[' || nextChar == ']'
                                || nextChar == '{' || nextChar == '}')
                            {
                                var token = CreateToken<Bracket>();
                                token.Raw = nextChar.ToString();
                                token.Type = nextChar;
                                yield return token;
                                _reader.Read();
                            }
                            else
                            {
                                throw SyntaxError("Unexpected character");
                            }
                            break;
                    }
                }

            }
            finally
            {
                _reader.Close();
            }
        }

        private Token LexString()
        {
            StringBuilder sb = new StringBuilder();

            var token = CreateToken<StringLiteral>();
            _reader.ResetRaw();
            _reader.Read(); // Skip the initial quote

            int next;
            while((next = _reader.Read()) != -1)
            {
                if (next == '\r' || next == '\n') throw SyntaxError("Unterminated string, encountered newline.");

                if (next == '\\')
                {
                    next = _reader.Read();

                    if (next == -1) goto Unexpected_String_EOF;

                    switch (next)
                    {
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case '0':
                            sb.Append('\0');
                            break;
                        case 'a':
                            sb.Append('\a');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'v':
                            sb.Append('\v');
                            break;
                        case '\'':
                        case '"':
                        case '\\':
                            sb.Append((char)next);
                            break;
                        case 'x':
                        case 'u':
                            int value = 0, i;
                            var fourDigitsRequired = next == 'u';
                            for (i = 0; i < 4; i++)
                            {
                                next = _reader.Peek();

                                if (next == -1) goto Unexpected_String_EOF;

                                var hValue = ((char) next).HexValue();
                                if (hValue >= 0)
                                {
                                    value = (value << 4) | hValue;
                                    _reader.Read();
                                }
                                else if (fourDigitsRequired)
                                {
                                    throw SyntaxError("Invalid hex escape sequence: four hex digit are required.");
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (i == 0) throw SyntaxError("Invalid hex escape sequence: at least one hex digit is required.");
                            sb.Append((char)value);
                            break;
                        // TODO: Add support for \U ?
                        default:
                            throw SyntaxError("Invalid escape sequence");
                    }
                }
                else if (next == '"')
                {
                    token.Raw = _reader.Raw;
                    token.Value = sb.ToString();
                    return token;
                }
                else
                {
                    sb.Append((char) next);
                }
            }

            Unexpected_String_EOF:
            throw SyntaxError("Unterminated string, encountered EOF.");
        }

        private Token LexIdentifier()
        {
            var token = CreateToken<Identifier>();
            _reader.ResetRaw();
            int next;
            while ((next = _reader.Peek()) != -1)
            {
                char c = (char) next;
                if (!char.IsLetter(c) && !char.IsDigit(c) && c != '_')
                {
                    break;
                }
                _reader.Read();
            }

            token.Raw = _reader.Raw;

            return token;
        }

        private Token LexWhitespace()
        {
            var token = CreateToken<Whitespace>();
            _reader.ResetRaw();
            int next;
            while ((next = _reader.Peek()) != -1)
            {
                char c = (char)next;
                if (!char.IsWhiteSpace(c))
                {
                    break;
                }
                _reader.Read();
            }

            token.Raw = _reader.Raw;

            return token;
        }

        private Token LexNumber()
        {
            var token = CreateToken<NumberLiteral>();
            _reader.ResetRaw();
            int next = _reader.Peek();

            var integerValue = 0UL;
            var exponent = 0;
            var exponentValue = 0;
            var gotFraction = false;
            var inExponent = false;
            var decPos = 0;
            var leadingZeros = 0;
            var trailingZeros = 0;
            byte digits = 0;
            var exponentSign = 1;

            if (next == '0')
            {
                _reader.Read();
                leadingZeros++;
                next = _reader.Peek();

                if (next == 'x' || next == 'X')
                {
                    integerValue = LexSpecialInteger(16);
                    goto result;
                }
                if (next == 'b' || next == 'B')
                {
                    integerValue = LexSpecialInteger(2);
                    goto result;
                }
                if (next == 'o' || next == 'O')
                {
                    integerValue = LexSpecialInteger(8);
                    goto result;
                }
            }

            while ((next = _reader.Peek()) != -1)
            {
                char c = (char)next;

                if (c == '.')
                {
                    if (gotFraction)
                    {
                        throw SyntaxError("Double decimal point.");
                    }

                    decPos = _reader.Raw.Length;
                    gotFraction = true;
                    _reader.Read();
                    continue;
                }

                if (c == 'e' || c == 'E')
                {
                    _reader.Read();
                    c = (char)_reader.Peek();

                    if (c == '+' || c == '-')
                    {
                        _reader.Read();

                        if (c == '-')
                        {
                            exponentSign = -1;
                        }
                    }

                    inExponent = true;
                    break;
                }

                if (!char.IsLetterOrDigit(c))
                {
                    break;
                }
                
                    if (c < '0' || c > '9')
                    {
                        throw SyntaxError("Invalid digit in number literal: {0}", c);
                    }

                if (c == '0')
                {
                    if (digits > 0)
                    {
                        trailingZeros++;
                    }
                    else
                    {
                        leadingZeros++;
                    }
                }
                else
                {
                    while (trailingZeros > 0)
                    {
                        integerValue *= 10;
                        trailingZeros--;
                        digits++;
                    }

                    integerValue = (ulong)(c - '0') + integerValue * 10;
                    digits++;
                }

                _reader.Read();
            }

            if (gotFraction)
            {
                exponent = decPos - leadingZeros;
            }
            else
            {
                exponent = digits + trailingZeros;
            }

            if (inExponent)
            {

                while ((next = _reader.Peek()) != -1)
                {
                    char c = (char) next;

                    if (c >= '0' && c <= '9')
                    {
                        exponentValue = (c - '0') + exponentValue * 10;
                        _reader.Read();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            exponent += exponentSign * exponentValue;

            result:
            token.Raw = _reader.Raw;
            token.Digits = integerValue;
            token.Exponent = exponent;
            token.DigitCount = digits;
            token.IsInteger = !(gotFraction || exponentValue < 0);

            return token;
        }

        private ulong LexSpecialInteger(byte intBase = 10)
        {
            int next;
            var integerValue = 0UL;

            while ((next = _reader.Peek()) != -1)
            {
                char c = (char)next;

                if (intBase == 2)
                {
                    if (c != '0' && c != '1')
                    {
                        throw SyntaxError("Invalid digit in binary integer literal: {0}", c);
                    }

                    integerValue = (byte)(c - '0') | (integerValue << 1);
                }
                else if (intBase == 8)
                {
                    if (c < '0' || c > '7')
                    {
                        throw SyntaxError("Invalid digit in octal integer literal: {0}", c);
                    }

                    integerValue = (byte)(c - '0') | (integerValue << 2);
                }
                else // intBase == 16
                {
                    var hexValue = c.HexValue();
                    if (hexValue < 0)
                    {
                        throw SyntaxError("Invalid digit in hex integer literal: {0}", c);
                    }

                    integerValue = (byte)hexValue | (integerValue << 4);
                }

                _reader.Read();
            }

            return integerValue;
        }

        private T CreateToken<T>()
            where T : Token, new()
        {
            return new T
            {
                Line = _reader.LinePos,
                Column = _reader.ColumnPos,
                Offset = _reader.Offset
            };
        }


        private SyntaxError SyntaxError(string msg, params object[] args)
        {
            return new SyntaxError(string.Format(msg, args), _reader.LinePos, _reader.ColumnPos, _reader.Offset);
        }
    }

    public static class Extensions
    {
        public static bool IsHex(this char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }

        public static int HexValue(this char c)
        {
            return (c >= '0' && c <= '9')
                ? c - '0'
                : (c >= 'a' && c <= 'f')
                    ? c - 'a' + 10
                    : (c >= 'A' && c <= 'F')
                        ? c - 'A' + 10
                        : -1;
        }

    }
}

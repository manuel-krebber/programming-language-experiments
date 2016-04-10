using System;
using System.IO;
using System.Linq;
using ProgrammingLanguage;
using Xunit;

namespace Test
{
    public class LexerTest
    {
        [Theory]
        [InlineData("0",        0,      0,  0, true)]
        [InlineData("0.5",      5,      1,  0, false)]
        [InlineData("10000.80", 100008, 6,  5, false)]
        [InlineData("10000",    1,      1,  5, true)]
        [InlineData("10005",    10005,  5,  5, true)]
        [InlineData("0.003",    3,      1, -2, false)]
        [InlineData("0.00300",  3,      1, -2, false)]
        [InlineData("8.15602",  815602, 6,  1, false)]
        [InlineData("1E4",      1,      1,  5, true)]
        [InlineData("1.5E4",    15,     2,  5, false)]
        [InlineData("15E4",     15,     2,  6, true)]
        [InlineData("15E+4",    15,     2,  6, true)]
        [InlineData("15E-4",    15,     2, -2, true)]
        public void TestLexNumber(string input, ulong digits, byte digitCount, int exponent, bool isInteger)
        {
            var lexer = new Lexer();
            var token = lexer.Tokenize(GenerateStreamFromString(input)).First();

            Assert.IsType<NumberLiteral>(token);

            var nt = (NumberLiteral) token;

            Assert.Equal(digits, nt.Digits);
            Assert.Equal(digitCount, nt.DigitCount);
            Assert.Equal(exponent, nt.Exponent);
            Assert.Equal(isInteger, nt.IsInteger);
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}

using System.IO;
using System.Text;

namespace ProgrammingLanguage
{
    public class PositionAwareReader : StreamReader
    {
        public PositionAwareReader(Stream stream) : base(stream)
        {
        }

        private readonly StringBuilder _raw = new StringBuilder();

        public override int Read()
        {
            var c = base.Read();
            if (c < 0) return c;

            _raw.Append((char)c);

            Offset++;

            var isNewline = c == '\n' || (c == '\r' && Peek() != '\n');

            if (isNewline)
            {
                LinePos++;
                ColumnPos = 0;
            }
            else
            {
                ColumnPos++;
            }

            return c;
        }

        public string Raw => _raw.ToString();

        public void ResetRaw()
        {
            _raw.Clear();
        }

        public uint LinePos { get; private set; }

        public uint ColumnPos { get; private set; }

        public uint Offset { get; private set; }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgrammingLanguage.Grammar;
using Antlr4.Runtime;
using LLVMSharp;

namespace ProgrammingLanguage
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            LLVMModuleRef module = LLVM.ModuleCreateWithName("my cool jit");
            LLVMBuilderRef builder = LLVM.CreateBuilder();

            var five = LLVM.ConstReal(LLVM.DoubleType(), 5);
            var three = LLVM.ConstReal(LLVM.DoubleType(), 3);

            var mult = LLVM.BuildFMul(builder, five, three, "multmp");

            LLVM.DumpValue(mult);
            LLVM.DumpModule(module);
            */

            var lexer = new Lexer();

            // var tokens = lexer.Tokenize(GenerateStreamFromString(@"""\u0022"" ab1_5 x8  ""9g\n\\n""")).ToArray();
            var tokens = lexer.Tokenize(GenerateStreamFromString("((0.5 )[10000.80 10000 10005 0.003 8.15602")).ToArray();

            Console.ReadLine();
        }

        public static Stream GenerateStreamFromString(string s)
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

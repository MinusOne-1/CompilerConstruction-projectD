using System.Globalization;
using System.Reflection;
using Compiler.Core.CodeAnalysis.LexicalAnalysis;

namespace Compiler.Core;

public static class Entrypoint
{
   /* public class Program
    {
        public readonly Type Type;
        public readonly MethodInfo Entrypoint;

        public Program(Type type, MethodInfo entrypoint)
        {
            Type = type;
            Entrypoint = entrypoint;
        }

        public TReturn Call<TReturn>(params object[] args) =>
            (TReturn)Entrypoint.Invoke(Type, args)!;
    }*/

    public static void Main(string[] args)
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US", false);
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);
        var programText = ("var a = 2;");
        var TestLexer = new Lexer(programText);
        for (int i = 0; i < TestLexer.ProgramTokens.Count;i++)
            Console.WriteLine(TestLexer.ProgramTokens[i]);
        // ImperativeCompiler.Compile(ProgramText, "BabylonianSqrt", "../BabylonianSqrt.dll");
    }
}
#if HAS_CODEFIX_TESTING
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PerformancePitfalls;

public class UnusedReturnValueCodeFixTests
{
    private static string DepsAttributes => "".AddSailfishAttributeDependencies();
    private static string DepsConsumer => @"
namespace Sailfish.Utilities {
    public static class Consumer {
        public static void Consume<T>(T value) { }
    }
}
";

    [Fact]
    public async Task Invocation_Result_Ignored_Is_Wrapped_In_Consumer()
    {
        const string testCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    class C { public int M() => 42; }\r\n" +
            "\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        new C().M();\r\n" +
            "    }\r\n" +
            "}\r\n";

        const string fixedCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "using Sailfish.Utilities;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    class C { public int M() => 42; }\r\n" +
            "\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        Consumer.Consume(new C().M());\r\n" +
            "    }\r\n" +
            "}\r\n";

        var test = new CSharpCodeFixTest<UnusedReturnValueAnalyzer, UnusedReturnValueCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("DepsB.cs", DepsConsumer), ("Test.cs", testCode) },
            },
            FixedState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("DepsB.cs", DepsConsumer), ("Test.cs", fixedCode) },
            },
        };

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(UnusedReturnValueAnalyzer.Descriptor).WithSpan("Test.cs", 11, 9, 11, 20).WithArguments("M"));
        await test.RunAsync();
    }
}

#endif


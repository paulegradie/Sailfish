#if HAS_CODEFIX_TESTING
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PerformancePitfalls;

public class EmptyLoopBodyCodeFixTests
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
    public async Task For_With_Semicolon_Gets_Consume_In_Block()
    {
        const string testCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        for (int i = 0; i < 3; i++) ;\r\n" +
            "    }\r\n" +
            "}\r\n";

        const string fixedCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "using Sailfish.Utilities;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        for (int i = 0; i < 3; i++)\r\n" +
            "        {\r\n" +
            "            Consumer.Consume(0);\r\n" +
            "        }\r\n" +
            "    }\r\n" +
            "}\r\n";

        var test = new CSharpCodeFixTest<EmptyLoopBodyAnalyzer, EmptyLoopBodyCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor).WithSpan("Test.cs", 9, 9, 9, 12));
        await test.RunAsync();
    }

    [Fact]
    public async Task While_With_Semicolon_Gets_Consume_In_Block()
    {
        const string testCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        while (false) ;\r\n" +
            "    }\r\n" +
            "}\r\n";

        const string fixedCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "using Sailfish.Utilities;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        while (false)\r\n" +
            "        {\r\n" +
            "            Consumer.Consume(0);\r\n" +
            "        }\r\n" +
            "    }\r\n" +
            "}\r\n";

        var test = new CSharpCodeFixTest<EmptyLoopBodyAnalyzer, EmptyLoopBodyCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor).WithSpan("Test.cs", 9, 9, 9, 14));
        await test.RunAsync();
    }

    [Fact]
    public async Task ForEachVariable_Deconstruction_Gets_Consume_In_Block()
    {
        const string testCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        foreach (var (a, b) in new[] { (1, 2) }) { }\r\n" +
            "    }\r\n" +
            "}\r\n";

        const string fixedCode = "\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "using Sailfish.Utilities;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        foreach (var (a, b) in new[] { (1, 2) }) {\r\n" +
            "            Consumer.Consume(0);\r\n" +
            "        }\r\n" +
            "    }\r\n" +
            "}\r\n";

        var test = new CSharpCodeFixTest<EmptyLoopBodyAnalyzer, EmptyLoopBodyCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor).WithSpan("Test.cs", 9, 9, 9, 16));
        await test.RunAsync();
    }
}

#endif

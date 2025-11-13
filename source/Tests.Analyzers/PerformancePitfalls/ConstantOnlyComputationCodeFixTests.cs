#if HAS_CODEFIX_TESTING
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PerformancePitfalls;

public class ConstantOnlyComputationCodeFixTests
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
    public async Task Adds_Consume_After_ConstantExpression_In_Block()
    {
        var testCode = ("\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        var x = 1 + 2;\r\n" +
            "    }\r\n" +
            "}\r\n").NormalizeLineEndings();

        var fixedCode = ("\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "using Sailfish.Utilities;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        var x = 1 + 2;\r\n" +
            "        Consumer.Consume((1 + 2));\r\n" +
            "    }\r\n" +
            "}\r\n").NormalizeLineEndings();

        var test = new CSharpCodeFixTest<ConstantOnlyComputationAnalyzer, ConstantOnlyComputationCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(ConstantOnlyComputationAnalyzer.Descriptor).WithSpan("Test.cs", 9, 17, 9, 22).WithArguments("Run"));
        await test.RunAsync();
    }

    [Fact]
    public async Task Wraps_Embedded_Statement_And_Adds_Consume()
    {
        var testCode = ("\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        if (true)\r\n" +
            "            _ = 3 * 7;\r\n" +
            "    }\r\n" +
            "}\r\n").NormalizeLineEndings();

        var fixedCode = ("\r\n" +
            "using Sailfish.AnalyzerTests;\r\n" +
            "using Sailfish.Utilities;\r\n" +
            "[Sailfish]\r\n" +
            "public class Bench\r\n" +
            "{\r\n" +
            "    [SailfishMethod]\r\n" +
            "    public void Run()\r\n" +
            "    {\r\n" +
            "        if (true)\r\n" +
            "        {\r\n" +
            "            _ = 3 * 7;\r\n" +
            "            Consumer.Consume((3 * 7));\r\n" +
            "        }\r\n" +
            "    }\r\n" +
            "}\r\n").NormalizeLineEndings();

        var test = new CSharpCodeFixTest<ConstantOnlyComputationAnalyzer, ConstantOnlyComputationCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(ConstantOnlyComputationAnalyzer.Descriptor).WithSpan("Test.cs", 10, 17, 10, 22).WithArguments("Run"));
        await test.RunAsync();
    }
}

#endif

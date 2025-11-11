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
    private static string Deps => "".AddSailfishAttributeDependencies() + @"
namespace Sailfish.Utilities {
    public static class Consumer {
        public static void Consume<T>(T value) { }
    }
}
";

    [Fact]
    public async Task Adds_Consume_After_ConstantExpression_In_Block()
    {
        const string testCode = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        var x = 1 + 2;
    }
}
";

        const string fixedCode = @"using Sailfish.Utilities;
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        var x = 1 + 2;
        Consumer.Consume((1 + 2));
    }
}
";

        var test = new CSharpCodeFixTest<ConstantOnlyComputationAnalyzer, ConstantOnlyComputationCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { ("Deps.cs", Deps), ("Test.cs", testCode) },
            },
            FixedState =
            {
                Sources = { ("Deps.cs", Deps), ("Test.cs", fixedCode) },
            },
        };

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(ConstantOnlyComputationAnalyzer.Descriptor));
        await test.RunAsync();
    }

    [Fact]
    public async Task Wraps_Embedded_Statement_And_Adds_Consume()
    {
        const string testCode = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        if (true)
            _ = 3 * 7;
    }
}
";

        const string fixedCode = @"using Sailfish.Utilities;
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        if (true)
        {
            _ = 3 * 7;
            Consumer.Consume((3 * 7));
        }
    }
}
";

        var test = new CSharpCodeFixTest<ConstantOnlyComputationAnalyzer, ConstantOnlyComputationCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { ("Deps.cs", Deps), ("Test.cs", testCode) },
            },
            FixedState =
            {
                Sources = { ("Deps.cs", Deps), ("Test.cs", fixedCode) },
            },
        };

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(ConstantOnlyComputationAnalyzer.Descriptor));
        await test.RunAsync();
    }
}

#endif

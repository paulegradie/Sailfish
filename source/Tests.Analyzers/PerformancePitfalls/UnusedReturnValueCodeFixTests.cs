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
    private static string Deps => "".AddSailfishAttributeDependencies() + @"
namespace Sailfish.Utilities {
    public static class Consumer {
        public static void Consume<T>(T value) { }
    }
}
";

    [Fact]
    public async Task Invocation_Result_Ignored_Is_Wrapped_In_Consumer()
    {
        const string testCode = @"
[Sailfish]
public class Bench
{
    class C { public int M() => 42; }

    [SailfishMethod]
    public void Run()
    {
        new C().M();
    }
}
";

        const string fixedCode = @"using Sailfish.Utilities;
[Sailfish]
public class Bench
{
    class C { public int M() => 42; }

    [SailfishMethod]
    public void Run()
    {
        Consumer.Consume(new C().M());
    }
}
";

        var test = new CSharpCodeFixTest<UnusedReturnValueAnalyzer, UnusedReturnValueCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(UnusedReturnValueAnalyzer.Descriptor));
        await test.RunAsync();
    }
}

#endif


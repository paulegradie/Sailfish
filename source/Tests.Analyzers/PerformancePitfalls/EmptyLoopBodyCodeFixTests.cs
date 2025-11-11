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
    private static string Deps => "".AddSailfishAttributeDependencies() + @"
namespace Sailfish.Utilities {
    public static class Consumer {
        public static void Consume<T>(T value) { }
    }
}
";

    [Fact]
    public async Task For_With_Semicolon_Gets_Consume_In_Block()
    {
        const string testCode = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        for (int i = 0; i < 3; i++) ;
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
        for (int i = 0; i < 3; i++)
        {
            Consumer.Consume(0);
        }
    }
}
";

        var test = new CSharpCodeFixTest<EmptyLoopBodyAnalyzer, EmptyLoopBodyCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor));
        await test.RunAsync();
    }

    [Fact]
    public async Task While_With_Semicolon_Gets_Consume_In_Block()
    {
        const string testCode = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        while (false) ;
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
        while (false)
        {
            Consumer.Consume(0);
        }
    }
}
";

        var test = new CSharpCodeFixTest<EmptyLoopBodyAnalyzer, EmptyLoopBodyCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor));
        await test.RunAsync();
    }

    [Fact]
    public async Task ForEachVariable_Deconstruction_Gets_Consume_In_Block()
    {
        const string testCode = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        foreach (var (a, b) in new[] { (1, 2) }) { }
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
        foreach (var (a, b) in new[] { (1, 2) })
        {
            Consumer.Consume(0);
        }
    }
}
";

        var test = new CSharpCodeFixTest<EmptyLoopBodyAnalyzer, EmptyLoopBodyCodeFixProvider, XUnitVerifier>
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

        test.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor));
        await test.RunAsync();
    }
}

#endif

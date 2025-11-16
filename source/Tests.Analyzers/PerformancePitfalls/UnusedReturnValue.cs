using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PerformancePitfalls;

public class UnusedReturnValue
{
    [Fact]
    public async Task Flags_NonVoidInvocation_Ignored()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        {|#0:Foo()|};
    }

    private int Foo() => 42;
}
";
        await AnalyzerVerifier<UnusedReturnValueAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(UnusedReturnValueAnalyzer.Descriptor).WithLocation(0).WithArguments("Foo"));
    }

    [Fact]
    public async Task DoesNotFlag_VoidInvocation()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        Bar();
    }

    private void Bar() { }
}
";
        await AnalyzerVerifier<UnusedReturnValueAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task Flags_Awaited_TaskOfT_Ignored()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public async Task Run()
    {
        {|#0:await TaskReturning()|};
    }

    private Task<int> TaskReturning() => Task.FromResult(1);
}
";
        await AnalyzerVerifier<UnusedReturnValueAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(UnusedReturnValueAnalyzer.Descriptor).WithLocation(0).WithArguments("TaskReturning"));
    }

    [Fact]
    public async Task DoesNotFlag_AssignedResult()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        var x = Foo();
    }

    private int Foo() => 42;
}
";
        await AnalyzerVerifier<UnusedReturnValueAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }
}


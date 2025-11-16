using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PerformancePitfalls;

public class EmptyLoopBody
{
    [Fact]
    public async Task Flags_EmptyForBlock()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        {|#0:for|} (int i = 0; i < 10; i++) { }
    }
}
";
        await AnalyzerVerifier<EmptyLoopBodyAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor).WithLocation(0));
    }

    [Fact]
    public async Task Flags_EmptyForWithSemicolon()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        {|#0:for|} (int i = 0; i < 10; i++) ;
    }
}
";
        await AnalyzerVerifier<EmptyLoopBodyAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor).WithLocation(0));
    }

    [Fact]
    public async Task DoesNotFlag_NonEmptyLoop()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        for (int i = 0; i < 10; i++) { DoWork(); }
    }

    private void DoWork() {}
}
";
        await AnalyzerVerifier<EmptyLoopBodyAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }
    [Fact]
    public async Task Flags_EmptyWhileWithSemicolon()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        {|#0:while|} (false) ;
    }
}
";
        await AnalyzerVerifier<EmptyLoopBodyAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor).WithLocation(0));
    }

    [Fact]
    public async Task Flags_EmptyForeachVariableDeconstruction()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        var arr = new (int, int)[] { (1, 2), (3, 4) };
        {|#0:foreach|} (var (a, b) in arr) { }
    }
}
";
        await AnalyzerVerifier<EmptyLoopBodyAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(EmptyLoopBodyAnalyzer.Descriptor).WithLocation(0));
    }

}


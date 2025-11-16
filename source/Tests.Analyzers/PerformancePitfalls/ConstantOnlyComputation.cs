using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PerformancePitfalls;

public class ConstantOnlyComputation
{
    [Fact]
    public async Task Flags_ConstantArithmeticOnly()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        var x = {|#0:1 + 2|};
        var y = x + 3; // mixed with non-constant local, but one constant-only op exists
    }
}
";
        await AnalyzerVerifier<ConstantOnlyComputationAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ConstantOnlyComputationAnalyzer.Descriptor).WithLocation(0).WithArguments("Run"));
    }

    [Fact]
    public async Task DoesNotFlag_When_UsesParameterOrField()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    private int _n = 10;

    [SailfishMethod]
    public void Run(int p = 5)
    {
        var z = p + 1; // uses parameter => not constant-only
        var w = _n + 2; // uses field => not constant-only
    }
}
";
        await AnalyzerVerifier<ConstantOnlyComputationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlag_When_UsesExternalCall()
    {
        const string source = @"
[Sailfish]
public class Bench
{
    [SailfishMethod]
    public void Run()
    {
        var t = System.DateTime.Now.Ticks; // external state
    }
}
";
        await AnalyzerVerifier<ConstantOnlyComputationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }
}


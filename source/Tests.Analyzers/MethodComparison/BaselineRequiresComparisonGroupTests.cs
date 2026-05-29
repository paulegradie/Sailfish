using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.MethodComparison;

public class BaselineRequiresComparisonGroupTests
{
    [Fact]
    public async Task ReportsErrorWhenIsBaselineWithoutComparisonGroup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod({|#0:IsBaseline = true|})]
    public void Orphan() { }

    [SailfishMethod]
    public void Other() { }
}";
        await AnalyzerVerifier<BaselineRequiresComparisonGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(BaselineRequiresComparisonGroupAnalyzer.Descriptor)
                .WithLocation(0)
                .WithArguments("Orphan"));
    }

    [Fact]
    public async Task NoDiagnosticWhenBaselineHasComparisonGroup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod(ComparisonGroup = ""g"", IsBaseline = true)]
    public void Baseline() { }

    [SailfishMethod(ComparisonGroup = ""g"")]
    public void Contender() { }
}";
        await AnalyzerVerifier<BaselineRequiresComparisonGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NoDiagnosticWhenIsBaselineIsFalse()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod(IsBaseline = false)]
    public void Plain() { }
}";
        await AnalyzerVerifier<BaselineRequiresComparisonGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }
}

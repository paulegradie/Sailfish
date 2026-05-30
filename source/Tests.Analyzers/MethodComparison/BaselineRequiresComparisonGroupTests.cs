using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.MethodComparison;

public class BaselineRequiresComparisonGroupTests
{
    [Fact]
    public async Task ReportsErrorWhenIsBaselineOnMethodNotInAnyGroup()
    {
        // Class has DisableComparison = true AND method has no explicit ComparisonGroup
        // → method is not in any comparison group → IsBaseline is invalid.
        const string source = @"
[Sailfish(DisableComparison = true)]
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
    public async Task NoDiagnosticWhenBaselineHasExplicitComparisonGroup()
    {
        // Explicit ComparisonGroup wins regardless of class-level setting.
        const string source = @"
[Sailfish(DisableComparison = true)]
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
    public async Task NoDiagnosticWhenBaselineJoinsImplicitClassGroup()
    {
        // [Sailfish] (no DisableComparison) means the method joins the implicit class-wide
        // comparison group, so IsBaseline is meaningful even without an explicit ComparisonGroup.
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod(IsBaseline = true)]
    public void Baseline() { }

    [SailfishMethod]
    public void Contender() { }
}";
        await AnalyzerVerifier<BaselineRequiresComparisonGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NoDiagnosticWhenIsBaselineIsFalse()
    {
        const string source = @"
[Sailfish(DisableComparison = true)]
public class TestCode
{
    [SailfishMethod(IsBaseline = false)]
    public void Plain() { }
}";
        await AnalyzerVerifier<BaselineRequiresComparisonGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }
}

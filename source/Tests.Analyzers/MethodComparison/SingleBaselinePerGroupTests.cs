using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.MethodComparison;

public class SingleBaselinePerGroupTests
{
    [Fact]
    public async Task ReportsErrorWhenTwoMethodsInSameGroupAreBaselines()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod(ComparisonGroup = ""g"", {|#0:IsBaseline = true|})]
    public void First() { }

    [SailfishMethod(ComparisonGroup = ""g"", {|#1:IsBaseline = true|})]
    public void Second() { }
}";
        await AnalyzerVerifier<SingleBaselinePerGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(SingleBaselinePerGroupAnalyzer.Descriptor).WithLocation(0).WithArguments("g", 2),
            new DiagnosticResult(SingleBaselinePerGroupAnalyzer.Descriptor).WithLocation(1).WithArguments("g", 2));
    }

    [Fact]
    public async Task NoDiagnosticWithExactlyOneBaseline()
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
        await AnalyzerVerifier<SingleBaselinePerGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task TwoBaselinesInDifferentGroupsIsAllowed()
    {
        // Different groups → no SF1301 because the rule is per (class, group).
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod(ComparisonGroup = ""a"", IsBaseline = true)]
    public void BaselineA() { }

    [SailfishMethod(ComparisonGroup = ""a"")]
    public void ContenderA() { }

    [SailfishMethod(ComparisonGroup = ""b"", IsBaseline = true)]
    public void BaselineB() { }

    [SailfishMethod(ComparisonGroup = ""b"")]
    public void ContenderB() { }
}";
        await AnalyzerVerifier<SingleBaselinePerGroupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }
}

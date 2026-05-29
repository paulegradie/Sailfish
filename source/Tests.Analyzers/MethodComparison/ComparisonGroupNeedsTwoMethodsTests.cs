using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.MethodComparison;

public class ComparisonGroupNeedsTwoMethodsTests
{
    [Fact]
    public async Task ReportsWarningForSoloMember()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod({|#0:ComparisonGroup = ""solo""|})]
    public void Lonely() { }

    [SailfishMethod]
    public void Unrelated() { }
}";
        await AnalyzerVerifier<ComparisonGroupNeedsTwoMethodsAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ComparisonGroupNeedsTwoMethodsAnalyzer.Descriptor)
                .WithLocation(0)
                .WithArguments("solo", "Lonely"));
    }

    [Fact]
    public async Task NoDiagnosticWhenGroupHasTwoOrMoreMembers()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod(ComparisonGroup = ""g"")]
    public void One() { }

    [SailfishMethod(ComparisonGroup = ""g"")]
    public void Two() { }
}";
        await AnalyzerVerifier<ComparisonGroupNeedsTwoMethodsAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }
}

using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.MethodComparison;

public class ComparisonGroupNeedsTwoMethodsTests
{
    [Fact]
    public async Task ReportsWarningForSoloMemberOfExplicitGroup()
    {
        // DisableComparison = true here so the unrelated method doesn't form its own implicit
        // single-member group and trigger a second diagnostic.
        const string source = @"
[Sailfish(DisableComparison = true)]
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
                .WithArguments("'solo'", "Lonely"));
    }

    [Fact]
    public async Task ReportsWarningForSoloMemberOfImplicitGroup()
    {
        // A [Sailfish] class with only one [SailfishMethod] forms an implicit group of size 1.
        const string source = @"
[Sailfish]
public class TestCode
{
    [{|#0:SailfishMethod|}]
    public void Lonely() { }
}";
        await AnalyzerVerifier<ComparisonGroupNeedsTwoMethodsAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ComparisonGroupNeedsTwoMethodsAnalyzer.Descriptor)
                .WithLocation(0)
                .WithArguments("(implicit class-wide)", "Lonely"));
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

    [Fact]
    public async Task NoDiagnosticForSoloMethodWhenClassDisablesComparison()
    {
        // Class opts out → the lone method doesn't form an implicit group → no SF1302.
        const string source = @"
[Sailfish(DisableComparison = true)]
public class TestCode
{
    [SailfishMethod]
    public void Alone() { }
}";
        await AnalyzerVerifier<ComparisonGroupNeedsTwoMethodsAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }
}

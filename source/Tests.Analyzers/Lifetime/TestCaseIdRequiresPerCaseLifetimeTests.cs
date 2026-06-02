using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.Lifetime;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.Lifetime;

public class TestCaseIdRequiresPerCaseLifetimeTests
{
    [Fact]
    public async Task ReportsErrorWhenTestCaseIdInjectedUnderDefaultLifetime()
    {
        // Default lifetime is SharedInstance: the constructor runs once, so an injected TestCaseId can't be per-case.
        const string source = @"
[Sailfish]
public class TestCode
{
    public TestCode({|#0:TestCaseId testCaseId|}) { }

    [SailfishMethod]
    public void M() { }
}";
        await AnalyzerVerifier<TestCaseIdRequiresPerCaseLifetimeAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TestCaseIdRequiresPerCaseLifetimeAnalyzer.Descriptor)
                .WithLocation(0)
                .WithArguments("TestCode"));
    }

    [Fact]
    public async Task ReportsErrorWhenTestCaseIdInjectedUnderExplicitSharedInstance()
    {
        const string source = @"
[Sailfish(Lifetime = SailfishLifetime.SharedInstance)]
public class TestCode
{
    public TestCode(ISomeDep dep, {|#0:TestCaseId testCaseId|}) { }

    [SailfishMethod]
    public void M() { }
}

public interface ISomeDep { }";
        await AnalyzerVerifier<TestCaseIdRequiresPerCaseLifetimeAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TestCaseIdRequiresPerCaseLifetimeAnalyzer.Descriptor)
                .WithLocation(0)
                .WithArguments("TestCode"));
    }

    [Fact]
    public async Task NoDiagnosticWhenLifetimeIsPerCase()
    {
        const string source = @"
[Sailfish(Lifetime = SailfishLifetime.PerCase)]
public class TestCode
{
    public TestCode(TestCaseId testCaseId) { }

    [SailfishMethod]
    public void M() { }
}";
        await AnalyzerVerifier<TestCaseIdRequiresPerCaseLifetimeAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NoDiagnosticWhenNoTestCaseIdParameter()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    public TestCode() { }

    [SailfishMethod]
    public void M() { }
}";
        await AnalyzerVerifier<TestCaseIdRequiresPerCaseLifetimeAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NoDiagnosticWhenNotASailfishClass()
    {
        // No [Sailfish] attribute → not a Sailfish test type → analyzer is silent.
        const string source = @"
public class TestCode
{
    public TestCode(TestCaseId testCaseId) { }
}";
        await AnalyzerVerifier<TestCaseIdRequiresPerCaseLifetimeAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }
}

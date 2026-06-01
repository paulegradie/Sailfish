using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.Trawl;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.Trawl;

public class TrawlAndSailfishMethodAreMutuallyExclusive
{
    [Fact]
    public async Task ReportsWhenMethodHasBothAttributes()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod]
    [Trawl]
    public void {|#0:DoWork|}()
    {
    }
}";
        await AnalyzerVerifier<TrawlAndSailfishMethodAreMutuallyExclusiveAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TrawlAndSailfishMethodAreMutuallyExclusiveAnalyzer.Descriptor).WithLocation(0).WithArguments("DoWork")
        );
    }

    [Fact]
    public async Task NoDiagnosticForTrawlOnly()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [Trawl]
    public void DoWork()
    {
    }
}";
        await AnalyzerVerifier<TrawlAndSailfishMethodAreMutuallyExclusiveAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies()
        );
    }

    [Fact]
    public async Task NoDiagnosticForSailfishMethodOnly()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishMethod]
    public void DoWork()
    {
    }
}";
        await AnalyzerVerifier<TrawlAndSailfishMethodAreMutuallyExclusiveAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies()
        );
    }
}

using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;
using System.Threading.Tasks;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PropertiesSetInGlobalSetup;

public class PropertiesShouldHavePublicSetters
{
    [Fact]
    public async Task ShouldDiscoverError()
    {
        const string source = @"
[Sailfish]
public class PrivateSettersShouldError
{
    public int {|#0:PrivateSetter|} { get; private set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        {|#1:PrivateSetter|} = 99;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}
";
        await AnalyzerVerifier<ShouldHavePublicSettersAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ShouldHavePublicSettersAnalyzer.Descriptor).WithLocation(0).WithArguments("PrivateSetter"),
            new DiagnosticResult(ShouldHavePublicSettersAnalyzer.Descriptor).WithLocation(1).WithArguments("PrivateSetter")
        );
    }

    [Fact]
    public async Task ShouldPass()
    {
        const string source = @"
[Sailfish]
public class TestClass
{
    public int PublicSetter { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        PublicSetter = 99;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}
";
        await AnalyzerVerifier<ShouldHavePublicSettersAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task OverridesShouldBeDiscoveredToo()
    {
        const string source = @"
[Sailfish]
class TestClass : BaseClass
{
    public int {|#0:PrivateSetter|} { get; private set; }

    protected override async Task MySetup(CancellationToken ct)
    {
        await Task.CompletedTask;
        {|#1:PrivateSetter|} = 99;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}

abstract class BaseClass
{
    protected virtual async Task MySetup(CancellationToken ct)
    {
        await Task.CompletedTask;
        // be overriden
    }

    [SailfishGlobalSetup]
    public async Task GlobalSetupBase(CancellationToken ct)
    {
        await MySetup(ct);
    }
}
";
        await AnalyzerVerifier<ShouldHavePublicSettersAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ShouldHavePublicSettersAnalyzer.Descriptor).WithLocation(0).WithArguments("PrivateSetter"),
            new DiagnosticResult(ShouldHavePublicSettersAnalyzer.Descriptor).WithLocation(1).WithArguments("PrivateSetter")
        );
    }
}
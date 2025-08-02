using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;
using System.Threading.Tasks;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PropertiesSetInGlobalSetup;

public class PropertiesShouldHavePublicGetters
{
    [Fact]
    public async Task ShouldDiscoverError()
    {
        const string source = @"
[Sailfish]
public class PrivateSettersShouldError
{
    public int {|#0:PrivateGetter|} { private get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        {|#1:PrivateGetter|} = 99;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}
";
        await AnalyzerVerifier<ShouldHavePublicGettersAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ShouldHavePublicGettersAnalyzer.Descriptor).WithLocation(0).WithArguments("PrivateGetter"),
            new DiagnosticResult(ShouldHavePublicGettersAnalyzer.Descriptor).WithLocation(1).WithArguments("PrivateGetter")
        );
    }

    [Fact]
    public async Task ShouldPass()
    {
        const string source = @"
[Sailfish]
public class TestMethod
{
    public int PublicGetter { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        PublicGetter = 99;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}
";
        await AnalyzerVerifier<ShouldHavePublicGettersAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }
}
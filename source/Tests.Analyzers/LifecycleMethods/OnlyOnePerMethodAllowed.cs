using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;
using Sailfish.Analyzers.Utils;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.LifecycleMethods;

public class OnlyOnePerMethodAllowed
{
    [Fact]
    public async Task ShouldDiscoverError()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishIterationSetup]
    [SailfishGlobalSetup]
    public void {|#0:GlobalSetup|}()
    {
    }

    [SailfishMethodSetup]
    public void SomeOtherTeardown()
    {
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await AnalyzerVerifier<OnlyOneLifecycleAttributePerMethod>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(Descriptors.OnlyOneLifecycleAttributePerMethod).WithLocation(0).WithArguments("GlobalSetup")
        );
    }
}
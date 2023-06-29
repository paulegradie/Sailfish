using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.Utils;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariablesShouldBePublicAnalyzer>;

namespace Tests.Sailfish.Analyzers;

public class TestClassPropertiesSetInGlobalSetupMustHavePublicSettersAndGetters
{
    [Fact]
    public async Task ShouldDiscoverError()
    {
        const string source = @"using Sailfish.Attributes;
using Shouldly;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish]
public class PrivateSettersShouldError
{
    public int {#|0:PrivateSetter|} { get; private set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        PrivateSetter = 99;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        PrivateSetter.ShouldBe(99);
    }
}";

        await Verify.VerifyAnalyzerAsync(
            source,
            new DiagnosticResult(Descriptors.PropertiesMustHavePublicSettersDescriptor).WithLocation(0));
    }
}
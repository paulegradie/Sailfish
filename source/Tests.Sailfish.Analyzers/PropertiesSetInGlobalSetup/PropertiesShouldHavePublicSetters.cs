using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;
using Sailfish.Analyzers.Utils;
using Tests.Sailfish.Analyzers.Utils;
using Xunit;

namespace Tests.Sailfish.Analyzers.PropertiesSetInGlobalSetup;

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
            new DiagnosticResult(Descriptors.PropertiesAssignedInGlobalSetupShouldHavePublicSettersDescriptor).WithLocation(0).WithArguments("PrivateSetter"),
            new DiagnosticResult(Descriptors.PropertiesAssignedInGlobalSetupShouldHavePublicSettersDescriptor).WithLocation(1).WithArguments("PrivateSetter")
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
}
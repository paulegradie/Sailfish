using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PropertiesSetInGlobalSetup;

public class PropertiesShouldBePublic
{
    [Fact]
    public async Task ShouldDiscoverError()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    int {|#0:NonPublicModifier|} { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        {|#1:NonPublicModifier|} = 77;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await AnalyzerVerifier<ShouldBePublicAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ShouldBePublicAnalyzer.Descriptor).WithLocation(0).WithArguments("NonPublicModifier"),
            new DiagnosticResult(ShouldBePublicAnalyzer.Descriptor).WithLocation(1).WithArguments("NonPublicModifier")
        );
    }

    [Fact]
    public async Task ShouldPass()
    {
        const string source = @"
[Sailfish]
public class PrivateSettersShouldError
{
    public int PublicModifier { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        PublicModifier = 77;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}

";
        await AnalyzerVerifier<ShouldBePublicAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task AttributelessHelperAssigningNonPublicPropertyShouldNotBeFlagged()
    {
        // Regression: a method with NO attributes must not be treated as a global-setup method.
        // Previously the .All(...) over an empty attribute sequence returned true, so attribute-less
        // helpers were scanned and produced a false-positive "must be public" diagnostic.
        const string source = @"
[Sailfish]
public class TestCode
{
    int NonPublicModifier { get; set; }

    private void Helper()
    {
        NonPublicModifier = 5;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await AnalyzerVerifier<ShouldBePublicAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task GlobalSetupWithAdditionalUnrelatedAttributeShouldStillBeDiscovered()
    {
        // The detection uses an intersection check, so a [SailfishGlobalSetup] method that also
        // carries an unrelated attribute (here [Obsolete]) must still be recognized as global setup.
        const string source = @"
[Sailfish]
public class TestCode
{
    int {|#0:NonPublicModifier|} { get; set; }

    [Obsolete]
    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        {|#1:NonPublicModifier|} = 77;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await AnalyzerVerifier<ShouldBePublicAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(ShouldBePublicAnalyzer.Descriptor).WithLocation(0).WithArguments("NonPublicModifier"),
            new DiagnosticResult(ShouldBePublicAnalyzer.Descriptor).WithLocation(1).WithArguments("NonPublicModifier")
        );
    }
}
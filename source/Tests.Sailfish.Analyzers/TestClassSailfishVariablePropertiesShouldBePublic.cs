using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers;
using Tests.Sailfish.Analyzers.Utils;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariablesShouldBePublicAnalyzer>;

namespace Tests.Sailfish.Analyzers;

public class TestClassSailfishVariablePropertiesShouldBePublic
{
    [Fact]
    public async Task WarningIsReturnedWhenPropertyIsNotPublic()
    {
        const string source = @"
[Sailfish]
public class WarningIsReturnedWhenPropertyIsNotPublic
{
    [SailfishVariable(1, 2, 3)] private int {|#0:Placeholder|} { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}
";
        await Verify.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(SailfishVariablesShouldBePublicAnalyzer.SailfishVariablesShouldBePublicDescriptor).WithLocation(0));
    }

    [Fact]
    public async Task NoWarningIsProducedWhenPropertyIsPublic()
    {
        const string source = @"[Sailfish]
public class NoWarningIsProducedWhenPropertyIsPublic
{
    [SailfishVariable(1, 2, 3)] public int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NonSailfishTestClassesDoNotCauseWarningWhenSailfishVariableAttributeIsApplied()
    {
        const string source = @"public class NonSailfishTestClassesDoNotCauseWarningWhenSailfishVariableAttributeIsApplied
{
    [SailfishVariable(1, 2, 3)] public int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NonSailfishTestClassesDoNotCauseWarnings()
    {
        const string source = @"public class NonSailfishTestClassesDoNotCauseWarnings
{
    int Placeholder { get; set; }

    public void MainMethod()
    {
        // do nothing
    }
}";
        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }
}
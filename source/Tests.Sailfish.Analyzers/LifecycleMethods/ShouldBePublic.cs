using Xunit;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;
using Sailfish.Analyzers.Utils;
using Tests.Sailfish.Analyzers.Utils;


namespace Tests.Sailfish.Analyzers.LifecycleMethods;

public class ShouldBePublic
{
    [Fact]
    public async Task ShouldNotError()
    {
        const string source = @"
[Sailfish]
public class MethodSpecificLifecycles
{
    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
    }

    [SailfishMethodSetup(nameof(TestOne))]
    public void ExecutionMethodSetup()
    {
    }

    [SailfishIterationSetup(nameof(TestOne), nameof(TestTwo))]
    public void IterationSetup()
    {
    }

    [SailfishMethod]
    public async Task TestOne(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestTwo(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishIterationTeardown(nameof(TestTwo))]
    public void IterationTeardown()
    {
    }

    [SailfishMethodTeardown]
    public async Task ExecutionMethodTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishGlobalTeardown]
    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    void TestMethod()
    {
    }
}
";

        await AnalyzerVerifier<LifecycleMethodsShouldBePublicAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies()
        );
    }

    [Fact]
    public async Task ShouldCatchAllErrors()
    {
        const string source = @"
[Sailfish]
public class MethodSpecificLifecycles
{
    [SailfishGlobalSetup]
    void {|#0:GlobalSetup|}()
    {
    }

    [SailfishMethodSetup(nameof(TestOne))]
    void {|#1:ExecutionMethodSetup|}()
    {
    }

    [SailfishIterationSetup(nameof(TestOne), nameof(TestTwo))]
    void {|#2:IterationSetup|}()
    {
    }

    [SailfishMethod]
    async Task {|#3:TestOne|}(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishMethod]
    async Task {|#4:TestTwo|}(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishIterationTeardown(nameof(TestTwo))]
    void {|#5:IterationTeardown|}()
    {
    }

    [SailfishMethodTeardown]
    async Task {|#6:ExecutionMethodTeardown|}(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishGlobalTeardown]
    async Task {|#7:GlobalTeardown|}(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    void TestMethod()
    {
    }
}
";

        await AnalyzerVerifier<LifecycleMethodsShouldBePublicAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(0),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(1),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(2),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(3),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(4),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(5),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(6),
            new DiagnosticResult(Descriptors.LifecycleMethodsShouldBePublic).WithLocation(7)
        );
    }
}
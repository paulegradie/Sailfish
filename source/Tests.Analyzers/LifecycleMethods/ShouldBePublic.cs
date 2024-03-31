using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;
using Sailfish.Analyzers.Utils;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.LifecycleMethods;

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
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(0).WithArguments("GlobalSetup"),
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(1).WithArguments("ExecutionMethodSetup"),
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(2).WithArguments("IterationSetup"),
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(3).WithArguments("TestOne"),
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(4).WithArguments("TestTwo"),
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(5).WithArguments("IterationTeardown"),
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(6).WithArguments("ExecutionMethodTeardown"),
            new DiagnosticResult(LifecycleMethodsShouldBePublicAnalyzer.Descriptor).WithLocation(7).WithArguments("GlobalTeardown")
        );
    }
}
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PropertiesSetInGlobalSetup;

public class VariableDependentStateInGlobalSetup
{
    [Fact]
    public async Task FlagsVariableDerivedAssignmentInGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _size;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _size = {|#0:N|} * 2;
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
    }

    [Fact]
    public async Task FlagsExpressionBodiedGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int[] _buffer = new int[0];

    [SailfishGlobalSetup]
    public void GlobalSetup() => _buffer = new int[{|#0:N|}];

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
    }

    [Fact]
    public async Task FlagsRangeVariableInGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishRangeVariable(1, 5)]
    public int N { get; set; }

    private int _size;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _size = {|#0:N|};
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
    }

    [Fact]
    public async Task FlagsSailfishVariablesInterfacePropertyInGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    public Sizes Size { get; set; }

    private Sizes _captured;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _captured = {|#0:Size|};
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}

public class Sizes : ISailfishVariables<int, SizesProvider>
{
}

public class SizesProvider
{
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("Size", "SailfishGlobalSetup"));
    }

    [Fact]
    public async Task FlagsVariableReadInGlobalTeardown()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _size;

    [SailfishGlobalTeardown]
    public void GlobalTeardown()
    {
        _size = {|#0:N|};
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalTeardown"));
    }

    [Fact]
    public async Task DoesNotDoubleReportMultipleReadsInTheSameStatement()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _size;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _size = {|#0:N|} + N;
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
    }

    [Fact]
    public async Task DoesNotFlagVariableUseInMethodSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _size;

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        _size = N * 2;
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagVariableUseInIterationSetupOrSailfishMethod()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _size;

    [SailfishIterationSetup]
    public void IterationSetup()
    {
        _size = N * 2;
    }

    [SailfishMethod]
    public void MainMethod()
    {
        var local = N + 1;
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagLiteralAssignmentInGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _size;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _size = 77;
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagNonVariablePropertyReadInGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    public int NotAVariable { get; set; }

    private int _size;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _size = NotAVariable * 2;
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagNameofVariableInGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private string _name;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _name = nameof(N);
    }

    [SailfishMethod]
    public void MainMethod()
    {
    }
}";
        await AnalyzerVerifier<VariableDependentStateInGlobalSetupAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }
}

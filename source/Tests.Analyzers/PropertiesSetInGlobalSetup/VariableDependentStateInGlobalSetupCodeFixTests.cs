#if HAS_CODEFIX_TESTING
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.PropertiesSetInGlobalSetup;

public class VariableDependentStateInGlobalSetupCodeFixTests
{
    private static string DepsAttributes => "".AddSailfishAttributeDependencies();

    [Fact]
    public async Task SolePurposeExpressionBodiedGlobalSetup_IsReAttributedAsMethodSetup()
    {
        var testCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int[] _buffer = new int[0];

    [SailfishGlobalSetup]
    public void Setup() => _buffer = new int[{|#0:N|}];

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var fixedCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int[] _buffer = new int[0];

    [SailfishMethodSetup]
    public void Setup() => _buffer = new int[N];

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var test = new CSharpCodeFixTest<VariableDependentStateInGlobalSetupAnalyzer, VariableDependentStateInGlobalSetupCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", testCode) },
            },
            FixedState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", fixedCode) },
            },
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
        await test.RunAsync();
    }

    [Fact]
    public async Task MixedGlobalSetup_MovesOnlyTheOffendingStatement_AndLeavesTheRest()
    {
        var testCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _connection;
    private int[] _buffer = new int[0];

    [SailfishGlobalSetup]
    public void Setup()
    {
        _connection = 5;
        _buffer = new int[{|#0:N|}];
    }

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var fixedCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _connection;
    private int[] _buffer = new int[0];

    [SailfishGlobalSetup]
    public void Setup()
    {
        _connection = 5;
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        _buffer = new int[N];
    }

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var test = new CSharpCodeFixTest<VariableDependentStateInGlobalSetupAnalyzer, VariableDependentStateInGlobalSetupCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", testCode) },
            },
            FixedState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", fixedCode) },
            },
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
        await test.RunAsync();
    }

    [Fact]
    public async Task MixedGlobalSetup_AppendsToAnExistingMethodSetup()
    {
        var testCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _connection;
    private int[] _buffer = new int[0];

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        _connection = 1;
    }

    [SailfishGlobalSetup]
    public void Setup()
    {
        _connection = 5;
        _buffer = new int[{|#0:N|}];
    }

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var fixedCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _connection;
    private int[] _buffer = new int[0];

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        _connection = 1;
        _buffer = new int[N];
    }

    [SailfishGlobalSetup]
    public void Setup()
    {
        _connection = 5;
    }

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var test = new CSharpCodeFixTest<VariableDependentStateInGlobalSetupAnalyzer, VariableDependentStateInGlobalSetupCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", testCode) },
            },
            FixedState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", fixedCode) },
            },
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
        await test.RunAsync();
    }

    [Fact]
    public async Task NestedAssignment_MovesTheWholeTopLevelStatement_WithoutDuplicating()
    {
        var testCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _connection;
    private int[] _buffer = new int[0];

    [SailfishGlobalSetup]
    public void Setup()
    {
        _connection = 5;
        if (_connection > 0)
        {
            _buffer = new int[{|#0:N|}];
        }
    }

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var fixedCode = (@"
using Sailfish.AnalyzerTests;
[Sailfish]
public class Bench
{
    [SailfishVariable(100, 1000, 10000)]
    public int N { get; set; }

    private int _connection;
    private int[] _buffer = new int[0];

    [SailfishGlobalSetup]
    public void Setup()
    {
        _connection = 5;
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        if (_connection > 0)
        {
            _buffer = new int[N];
        }
    }

    [SailfishMethod]
    public void Join()
    {
    }
}
").NormalizeLineEndings();

        var test = new CSharpCodeFixTest<VariableDependentStateInGlobalSetupAnalyzer, VariableDependentStateInGlobalSetupCodeFixProvider, XUnitVerifier>
        {
            TestState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", testCode) },
            },
            FixedState =
            {
                Sources = { ("DepsA.cs", DepsAttributes), ("Test.cs", fixedCode) },
            },
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(VariableDependentStateInGlobalSetupAnalyzer.Descriptor).WithLocation(0).WithArguments("N", "SailfishGlobalSetup"));
        await test.RunAsync();
    }
}
#endif

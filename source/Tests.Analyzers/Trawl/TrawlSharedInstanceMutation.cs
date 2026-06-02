using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.Trawl;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.Trawl;

public class TrawlSharedInstanceMutation
{
    [Fact]
    public async Task FlagsUnsynchronizedFieldWriteInTrawlMethod()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _counter;

    [Trawl]
    public void DoWork()
    {
        {|#0:_counter|} = _counter + 1;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TrawlSharedInstanceMutationAnalyzer.Descriptor).WithLocation(0).WithArguments("_counter", "DoWork"));
    }

    [Fact]
    public async Task FlagsCompoundAssignmentToField()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private long _sum;

    [Trawl]
    public void DoWork()
    {
        {|#0:_sum|} += 10;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TrawlSharedInstanceMutationAnalyzer.Descriptor).WithLocation(0).WithArguments("_sum", "DoWork"));
    }

    [Fact]
    public async Task FlagsIncrementToField()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _count;

    [Trawl]
    public void DoWork()
    {
        {|#0:_count|}++;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TrawlSharedInstanceMutationAnalyzer.Descriptor).WithLocation(0).WithArguments("_count", "DoWork"));
    }

    [Fact]
    public async Task FlagsThisQualifiedFieldWrite()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _counter;

    [Trawl]
    public void DoWork()
    {
        {|#0:this._counter|} = 5;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TrawlSharedInstanceMutationAnalyzer.Descriptor).WithLocation(0).WithArguments("_counter", "DoWork"));
    }

    [Fact]
    public async Task FlagsAutoPropertyWrite()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    public int Counter { get; set; }

    [Trawl]
    public void DoWork()
    {
        {|#0:Counter|} = 5;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(
            source.AddSailfishAttributeDependencies(),
            new DiagnosticResult(TrawlSharedInstanceMutationAnalyzer.Descriptor).WithLocation(0).WithArguments("Counter", "DoWork"));
    }

    [Fact]
    public async Task DoesNotFlagInterlockedGuardedWrite()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _counter;

    [Trawl]
    public void DoWork()
    {
        Interlocked.Increment(ref _counter);
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagLockGuardedWrite()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private readonly object _gate = new object();
    private int _counter;

    [Trawl]
    public void DoWork()
    {
        lock (_gate)
        {
            _counter++;
        }
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagReadOnlyFieldUsage()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private readonly int _max = 100;

    [Trawl]
    public void DoWork()
    {
        var local = _max + 1;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagFieldThatIsOnlyRead()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _value;

    [Trawl]
    public void DoWork()
    {
        var local = _value + 1;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagWriteInGlobalSetup()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _counter;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _counter = 0;
    }

    [Trawl]
    public void DoWork()
    {
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagLocalVariableWrite()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    [Trawl]
    public void DoWork()
    {
        var local = 0;
        local++;
        local = 5;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagPropertyWithCustomSetter()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _backing;
    public int Counter { get => _backing; set => _backing = value; }

    [Trawl]
    public void DoWork()
    {
        Counter = 5;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task DoesNotFlagWriteInSailfishMethod()
    {
        const string source = @"
[Sailfish]
public class TestCode
{
    private int _counter;

    [SailfishMethod]
    public void DoWork()
    {
        _counter++;
    }
}";
        await AnalyzerVerifier<TrawlSharedInstanceMutationAnalyzer>.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }
}

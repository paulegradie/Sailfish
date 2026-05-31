using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class CompiledInvokerTests
{
    [Fact]
    public async Task SyncVoid_IsInvoked()
    {
        var t = new Target();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.SyncVoid))!);
        await invoke(CancellationToken.None);
        t.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task SyncWithToken_ReceivesToken()
    {
        var t = new Target();
        using var cts = new CancellationTokenSource();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.SyncToken))!);
        await invoke(cts.Token);
        t.Calls.ShouldBe(1);
        t.LastToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task SyncNonVoid_IsInvoked_ReturnValueIgnored()
    {
        var t = new Target();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.SyncInt))!);
        await invoke(CancellationToken.None);
        t.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task TaskReturning_IsAwaited()
    {
        var t = new Target();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.AsyncTask))!);
        await invoke(CancellationToken.None);
        t.AsyncCompleted.ShouldBeTrue(); // only set after the await resumes
    }

    [Fact]
    public async Task TaskOfT_IsAwaited()
    {
        var t = new Target();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.AsyncTaskInt))!);
        await invoke(CancellationToken.None);
        t.AsyncCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ValueTaskReturning_IsAwaited()
    {
        var t = new Target();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.AsyncValueTask))!);
        await invoke(CancellationToken.None);
        t.AsyncCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ValueTaskOfT_IsAwaited()
    {
        var t = new Target();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.AsyncValueTaskInt))!);
        await invoke(CancellationToken.None);
        t.AsyncCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Exception_PropagatesUnwrapped()
    {
        var t = new Target();
        var invoke = CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.Throws))!);
        // Direct propagation — not wrapped in TargetInvocationException the way MethodInfo.Invoke would.
        await Should.ThrowAsync<InvalidOperationException>(async () => await invoke(CancellationToken.None));
    }

    [Fact]
    public void BadSignature_ThrowsTestFormatException()
    {
        var t = new Target();
        Should.Throw<TestFormatException>(() => CompiledInvoker.Build(t, typeof(Target).GetMethod(nameof(Target.TwoParams))!));
    }

    [Fact]
    public async Task Empty_Baseline_CompletesAsNoOp()
    {
        await CompiledInvoker.Empty(CancellationToken.None);
    }

    private sealed class Target
    {
        public int Calls;
        public bool AsyncCompleted;
        public CancellationToken LastToken;

        public void SyncVoid() => Calls++;
        public void SyncToken(CancellationToken ct) { Calls++; LastToken = ct; }
        public int SyncInt() { Calls++; return 42; }

        public async Task AsyncTask() { await Task.Yield(); AsyncCompleted = true; }
        public async Task<int> AsyncTaskInt() { await Task.Yield(); AsyncCompleted = true; return 1; }
        public async ValueTask AsyncValueTask() { await Task.Yield(); AsyncCompleted = true; }
        public async ValueTask<int> AsyncValueTaskInt() { await Task.Yield(); AsyncCompleted = true; return 1; }

        public void Throws() => throw new InvalidOperationException("boom");
        public void TwoParams(CancellationToken ct, int extra) { }
    }
}

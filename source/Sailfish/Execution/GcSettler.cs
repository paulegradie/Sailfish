using System;

namespace Sailfish.Execution;

/// <summary>
///     Forces a deterministic garbage collection between measured iterations so that memory pressure
///     (and any deferred finalization) built up by one iteration does not land inside the next
///     iteration's measured window and inflate it. This mirrors BenchmarkDotNet, which collects
///     between iterations by default. Opt-out via <c>ForceGcBetweenIterations = false</c>.
/// </summary>
internal static class GcSettler
{
    public static void Settle()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

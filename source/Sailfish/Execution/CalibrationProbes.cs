using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal static class CalibrationProbes
{
    // Synchronous no-op
    internal static void SyncNoop() { }

    // Synchronous no-op with CancellationToken
    internal static void SyncNoopToken(CancellationToken _)
    {
        // Intentionally empty
    }

    // Async no-op
    internal static Task AsyncNoop() => Task.CompletedTask;

    // Async no-op with CancellationToken
    internal static Task AsyncNoopToken(CancellationToken _) => Task.CompletedTask;
}


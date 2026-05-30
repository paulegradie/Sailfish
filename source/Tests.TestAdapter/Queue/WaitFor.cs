using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.TestAdapter.Queue;

internal static class WaitFor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);

    public static async Task ConditionAsync(Func<bool> condition, TimeSpan? timeout = null, string? description = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;
            await Task.Delay(PollInterval);
        }

        if (condition()) return;
        throw new TimeoutException($"Condition not met within {(timeout ?? DefaultTimeout).TotalMilliseconds:F0} ms: {description ?? "unspecified"}");
    }

    public static Task CountReachedAsync(Func<int> counter, int target, TimeSpan? timeout = null) =>
        ConditionAsync(() => counter() >= target, timeout, $"counter >= {target}");
}

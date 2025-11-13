using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal class HarnessBaselineCalibrator
{
    private const int WarmupCount = 16;
    private const int SampleCount = 64;


    // Exposed for diagnostics consumers
    internal static int Warmups => WarmupCount;
    internal static int Samples => SampleCount;

    public async Task<int> CalibrateTicksAsync(MethodInfo methodUnderTest, CancellationToken cancellationToken)
    {
        if (methodUnderTest == null) throw new ArgumentNullException(nameof(methodUnderTest));

        var probe = ResolveProbe(methodUnderTest);
        var perfTimer = new PerformanceTimer(); // used by TryInvoke to mirror real timing path

        // Warmup JIT/infra
        for (var i = 0; i < WarmupCount; i++)
        {
            await probe.TryInvoke(null, cancellationToken, perfTimer).ConfigureAwait(false);
        }

        // Measure N samples
        var samples = new List<long>(SampleCount);
        for (var i = 0; i < SampleCount; i++)
        {
            var sw = Stopwatch.StartNew();
            await probe.TryInvoke(null, cancellationToken, perfTimer).ConfigureAwait(false);
            sw.Stop();
            samples.Add(sw.ElapsedTicks);
        }

        if (samples.Count == 0) return 0;
        var median = Median(samples);

        // Clamp to int range and non-negative
        if (median < 0) median = 0;
        if (median > int.MaxValue) median = int.MaxValue;
        return (int)median;
    }

    private static MethodInfo ResolveProbe(MethodInfo methodUnderTest)
    {
        var isAsync = methodUnderTest.IsAsyncMethod();
        var parameters = methodUnderTest.GetParameters();
        var hasToken = parameters.Length == 1 && parameters[0].ParameterType == typeof(CancellationToken);

        var probeType = typeof(CalibrationProbes);
        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var types = hasToken ? [typeof(CancellationToken)] : Type.EmptyTypes;
        var name = isAsync
            ? (hasToken ? "AsyncNoopToken" : "AsyncNoop")
            : (hasToken ? "SyncNoopToken" : "SyncNoop");

        var mi = probeType.GetMethod(name, flags, binder: null, types: types, modifiers: null);
        if (mi == null)
            throw new InvalidOperationException($"Calibration probe method '{name}' not found.");
        return mi;
    }

    private static long Median(IReadOnlyList<long> values)
    {
        var ordered = values.OrderBy(v => v).ToArray();
        var n = ordered.Length;
        if (n % 2 == 1) return ordered[n / 2];
        var a = ordered[(n / 2) - 1];
        var b = ordered[n / 2];
        // average two middles safely
        return a + ((b - a) / 2);
    }
}


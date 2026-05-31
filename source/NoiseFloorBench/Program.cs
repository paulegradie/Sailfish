using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// ---------------------------------------------------------------------------
// Noise-floor A/B for Sailfish's measured-invocation path.
//
//   invoke-bare : MethodInfo.Invoke(instance, null)         (best-case reflection)
//   tryinvoke   : faithful replica of the legacy TryInvoke  (the real before-state:
//                 GetParameters().ToList(), new List<object>, GetType().Name,
//                 unconditional error-string interpolation, object[] per call)
//   delegate    : compiled direct call (the new CompiledInvoker path)
//
// Per-op time is measured with batched timing (the OperationsPerInvoke technique:
// loop N calls in one Stopwatch, divide by N). We also report bytes allocated per
// call. Lower overhead floor + lower variance + zero allocation => ability to
// resolve smaller differences (allocations show up as GC-driven outliers).
// ---------------------------------------------------------------------------

const int Inner = 200_000; // invocations per batch (amortizes timer resolution)
const int Warmup = 50;     // warmup batches
const int Batches = 80;    // measured batches

try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch { /* best effort */ }

var target = new Target();
var emptyMi = typeof(Target).GetMethod(nameof(Target.Empty))!;
var tinyMi = typeof(Target).GetMethod(nameof(Target.Tiny))!;

var paths = new (string Name, Action Empty, Action Tiny)[]
{
    ("invoke-bare", () => emptyMi.Invoke(target, null), () => tinyMi.Invoke(target, null)),
    ("tryinvoke",   () => LegacyTryInvoke(emptyMi, target), () => LegacyTryInvoke(tinyMi, target)),
    ("delegate",    BuildDirect(target, emptyMi), BuildDirect(target, tinyMi)),
};

var ticksToNs = 1_000_000_000.0 / Stopwatch.Frequency;

Console.WriteLine($"Stopwatch: {(Stopwatch.IsHighResolution ? "high-res" : "low-res")}, {Stopwatch.Frequency:N0} Hz (~{ticksToNs:F2} ns/tick)");
Console.WriteLine($"Inner={Inner:N0}/batch  Warmup={Warmup}  Batches={Batches}");
Console.WriteLine();
Console.WriteLine($"{"path",-13}{"workload",-9}{"median ns",11}{"mean ns",10}{"sd ns",9}{"CV %",8}{"p97.5",10}{"bytes/op",11}");
Console.WriteLine(new string('-', 81));

var results = new Dictionary<string, (Stats Empty, Stats Tiny, double Bytes)>();
foreach (var p in paths)
{
    var bytes = AllocBytesPerOp(p.Empty);
    var e = Measure(p.Name, "empty", p.Empty, bytes);
    var t = Measure(p.Name, "tiny", p.Tiny, bytes);
    results[p.Name] = (e, t, bytes);
}

Console.WriteLine();
Console.WriteLine("Delegate vs legacy tryinvoke");
Console.WriteLine(new string('-', 81));
var legacy = results["tryinvoke"];
var del = results["delegate"];
Console.WriteLine($"  overhead floor (empty median):  tryinvoke {legacy.Empty.Median,8:F2} ns   delegate {del.Empty.Median,7:F2} ns   => {Ratio(legacy.Empty.Median, del.Empty.Median)}");
Console.WriteLine($"  overhead noise (empty sd):       tryinvoke {legacy.Empty.StdDev,8:F2} ns   delegate {del.Empty.StdDev,7:F2} ns   => {Ratio(legacy.Empty.StdDev, del.Empty.StdDev)}");
Console.WriteLine($"  tail (empty p97.5):              tryinvoke {legacy.Empty.P975,8:F2} ns   delegate {del.Empty.P975,7:F2} ns   => {Ratio(legacy.Empty.P975, del.Empty.P975)}");
Console.WriteLine($"  allocation per call:             tryinvoke {legacy.Bytes,8:F0} B    delegate {del.Bytes,7:F0} B");
Console.WriteLine();
Console.WriteLine("The overhead floor is what gets estimated and subtracted from every sample; its size and");
Console.WriteLine("noise bound the smallest resolvable difference. Per-call allocations (legacy) surface as");
Console.WriteLine("GC-driven tail outliers that widen distributions and corrupt small-difference tests.");

return;

Stats Measure(string path, string name, Action invoke, double bytes)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    for (var w = 0; w < Warmup; w++) RunBatch(invoke);

    var perOp = new double[Batches];
    for (var b = 0; b < Batches; b++)
        perOp[b] = RunBatch(invoke) * ticksToNs / Inner;

    var s = Stats.From(perOp);
    Console.WriteLine($"{path,-13}{name,-9}{s.Median,11:F2}{s.Mean,10:F2}{s.StdDev,9:F2}{s.Cv,8:F1}{s.P975,10:F2}{bytes,11:F0}");
    return s;
}

long RunBatch(Action invoke)
{
    var sw = Stopwatch.StartNew();
    for (var i = 0; i < Inner; i++) invoke();
    sw.Stop();
    return sw.ElapsedTicks;
}

double AllocBytesPerOp(Action invoke)
{
    for (var i = 0; i < 1000; i++) invoke(); // settle
    const int n = 50_000;
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < n; i++) invoke();
    var after = GC.GetAllocatedBytesForCurrentThread();
    return (after - before) / (double)n;
}

// Faithful replica of Sailfish.Extensions.Methods.InvocationReflectionExtensionMethods.TryInvoke
// for a synchronous, parameterless method (the per-call work it does today).
static void LegacyTryInvoke(MethodInfo method, object instance)
{
    var parameters = method.GetParameters().ToList();
    var arguments = new List<object>();
    var className = instance.GetType().Name;
    var errorMsg = $"The '{method.Name}' method in class '{className}' may only receive a single 'CancellationToken' parameter";
    if (parameters.Count == 1) { /* would validate + add token */ }
    GC.KeepAlive(errorMsg);
    method.Invoke(instance, arguments.ToArray());
}

static Action BuildDirect(object instance, MethodInfo mi)
{
    var call = Expression.Call(Expression.Constant(instance), mi);
    return Expression.Lambda<Action>(call).Compile();
}

static string Ratio(double legacy, double del) =>
    del <= 0 ? "n/a" : $"{legacy / del,5:F1}x more for tryinvoke";

internal readonly record struct Stats(double Median, double Mean, double StdDev, double Cv, double P025, double P975)
{
    public static Stats From(double[] values)
    {
        var sorted = values.OrderBy(x => x).ToArray();
        var n = sorted.Length;
        var mean = sorted.Average();
        var variance = n > 1 ? sorted.Sum(x => (x - mean) * (x - mean)) / (n - 1) : 0;
        var sd = Math.Sqrt(variance);
        return new Stats(
            Median: Percentile(sorted, 50),
            Mean: mean,
            StdDev: sd,
            Cv: mean > 0 ? sd / mean * 100.0 : 0,
            P025: Percentile(sorted, 2.5),
            P975: Percentile(sorted, 97.5));
    }

    private static double Percentile(double[] sorted, double p)
    {
        if (sorted.Length == 0) return 0;
        if (sorted.Length == 1) return sorted[0];
        var rank = p / 100.0 * (sorted.Length - 1);
        var lo = (int)Math.Floor(rank);
        var hi = (int)Math.Ceiling(rank);
        if (lo == hi) return sorted[lo];
        return sorted[lo] + (rank - lo) * (sorted[hi] - sorted[lo]);
    }
}

internal sealed class Target
{
    public long Sink;

    public void Empty() { }

    // ~tens of ns of unelidable integer work (64-bit LCG mix, fed through an instance field)
    public void Tiny()
    {
        var x = Sink;
        for (var i = 0; i < 16; i++)
            x = x * 6364136223846793005L + 1442695040888963407L;
        Sink = x;
    }
}

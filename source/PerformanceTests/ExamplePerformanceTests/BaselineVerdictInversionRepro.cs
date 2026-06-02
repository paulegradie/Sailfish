using System.Linq;
using System.Text;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Demonstration/repro for the inverted-baseline reporting bug in the IDE comparison / SailDiff
/// output (originally surfaced on Sailfish.TestAdapter 4.0.136, now FIXED).
///
/// <para>
/// Ground truth: <see cref="WithPlus"/> is ~quadratic and the SLOWEST method; it is deliberately
/// flagged <c>IsBaseline = true</c>. <see cref="WithStringBuilder"/> is ~linear and the FASTEST.
/// A correct report must say WithStringBuilder is FASTER than the WithPlus baseline.
/// </para>
///
/// <para>
/// Before the fix, the IDE "IMPACT" lines inverted: rows whose CANDIDATE was the baseline-flagged
/// method came out as e.g. <c>"🟢 IMPACT: WithPlus(N: …) is 99.x% faster than baseline
/// WithStringBuilder(N: …)"</c> with the means transposed, and the report compared across different
/// N. The fix now reports each contender against the WithPlus baseline at the same N, e.g.
/// <c>"🟢 IMPACT: WithStringBuilder(N: 100) is 79.x% faster than baseline WithPlus(N: 100)"</c>.
/// See the deterministic reproduction in
/// <c>Tests.TestAdapter/Queue/MethodComparisonBatchProcessor_BaselineInversionTests.cs</c> and the
/// write-up in <c>docs/diagnosis/baseline-verdict-inversion.md</c>.
/// </para>
///
/// <para>
/// Kept <c>Disabled = true</c> so it never burdens CI with a multi-second O(n²) run. To exercise it
/// live, set <c>Disabled = false</c> and run in RELEASE so timings are clean (Debug inflates and adds
/// noise).
/// </para>
/// </summary>
[Sailfish(NumWarmupIterations = 3, SampleSize = 30, Disabled = true)]
public class BaselineVerdictInversionRepro
{
    [SailfishVariable(100, 10_000)]
    public int N { get; set; }

    private string[] _parts = [];

    [SailfishMethodSetup]
    public void Setup() => _parts = Enumerable.Range(0, N).Select(i => i.ToString()).ToArray();

    // SLOW: O(n^2) string concatenation. Deliberately flagged as the baseline.
    [SailfishMethod(ComparisonGroup = "Concat", IsBaseline = true)]
    public string WithPlus()
    {
        var result = string.Empty;
        for (var i = 0; i < _parts.Length; i++) result += _parts[i];
        return result;
    }

    // FAST: O(n) StringBuilder. A candidate.
    [SailfishMethod(ComparisonGroup = "Concat")]
    public string WithStringBuilder()
    {
        var sb = new StringBuilder(_parts.Length * 4);
        for (var i = 0; i < _parts.Length; i++) _ = sb.Append(_parts[i]);
        return sb.ToString();
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

/// <summary>
/// Runtime backstop for the "frozen scaling variable" trap. A <c>[SailfishVariable(scaleFish: true)]</c>
/// property is meant to drive the amount of timed work so ScaleFish can model how runtime grows with it.
/// If the N-dependent state is built in <c>[SailfishGlobalSetup]</c> — which runs once per class and is
/// cached/replayed across every variable set — every case measures the <em>first</em> N, the timing is
/// flat across the variable, and ScaleFish fits a near-constant curve. The benchmark looks ~O(1) even
/// though the code under test is not.
///
/// <para>
/// This detector inspects an <em>already-computed</em> <see cref="ScaleFishModel"/> (no curve refitting)
/// and decides whether the fit is effectively constant. It is intentionally a small pure function so the
/// decision can be unit-tested directly, independent of the run/reporting plumbing.
/// </para>
///
/// <para>
/// Detection is a <em>hint</em>, never an error: genuinely constant-time code (hash lookups, fixed-size
/// work, etc.) is perfectly legitimate. The reporting layer therefore phrases the result as a question.
/// </para>
/// </summary>
public static class ConstantComplexityDetector
{
    /// <summary>
    /// Default fraction by which the fitted curve must change across the measured X range before the
    /// variable is considered to be meaningfully driving runtime. A fitted curve that varies by less
    /// than 5% from its smallest to its largest measured X is treated as effectively flat (~O(1)).
    /// </summary>
    public const double DefaultRelativeSpanThreshold = 0.05;

    /// <summary>
    /// When the continuous power-log diagnostic is available, an exponent below this magnitude is treated
    /// as "no real power-law growth" — corroborating a constant classification. (b ≈ 0 ⇒ x^b ≈ 1.)
    /// </summary>
    public const double DefaultPowerExponentThreshold = 0.10;

    /// <summary>
    /// Decides whether the supplied fit looks effectively constant (~O(1)).
    ///
    /// <para>
    /// The primary, family-agnostic signal is the <em>relative span</em> of the fitted curve over the
    /// observed X range: every built-in family evaluates to <c>scale·Basis(x) + bias</c>, so when the
    /// measured timings are flat the fitted slope collapses toward zero and the curve barely moves between
    /// the smallest and largest measured N. We compute how much the curve changes across that range
    /// relative to its level; below <paramref name="relativeSpanThreshold"/> the variable is not
    /// meaningfully reaching the timed work.
    /// </para>
    ///
    /// <para>
    /// To avoid false positives on data that clearly <em>does</em> scale, a near-flat curve is only flagged
    /// when the classification is <em>not</em> statistically distinguishable
    /// (<see cref="ScaleFishModel.IsDistinguishable"/> is false) — i.e. the per-variable-value timing
    /// distributions cannot separate one growth family from another, which is exactly the signature of a
    /// frozen variable. A confidently-distinguishable scaling fit (clear Linear, Quadratic, …) is never
    /// flagged even if its measured span happens to be modest.
    /// </para>
    ///
    /// <para>
    /// When measurements are unavailable (e.g. a deserialised model with no raw points) the detector falls
    /// back to the continuous power-log exponent: an indistinguishable fit whose power exponent
    /// <c>b ≈ 0</c> is treated as constant. With neither measurements nor a usable power-log diagnostic the
    /// detector conservatively returns false rather than guessing.
    /// </para>
    /// </summary>
    /// <param name="model">The fit produced by the estimator. Must not be null.</param>
    /// <param name="measurements">
    /// The (X, Y) points the fit was computed from, used to bound the relative-span calculation to the
    /// range the user actually measured. May be null/empty, in which case the power-log fallback is used.
    /// </param>
    /// <param name="relativeSpanThreshold">Override for <see cref="DefaultRelativeSpanThreshold"/>.</param>
    /// <param name="powerExponentThreshold">Override for <see cref="DefaultPowerExponentThreshold"/>.</param>
    public static bool IsLikelyConstant(
        ScaleFishModel model,
        IReadOnlyList<ComplexityMeasurement>? measurements,
        double relativeSpanThreshold = DefaultRelativeSpanThreshold,
        double powerExponentThreshold = DefaultPowerExponentThreshold)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));

        // A fit the data can confidently separate into a specific growth family is, by definition, not the
        // ambiguous-flat signature we are looking for. Never second-guess a distinguishable classification.
        if (model.IsDistinguishable) return false;

        var function = model.ScaleFishModelFunction;
        var fit = function?.FunctionParameters;

        // Preferred signal: how much does the fitted curve move across the measured X range?
        if (function is not null && fit is not null && TryGetXRange(measurements, out var xMin, out var xMax))
        {
            double yLow, yHigh;
            try
            {
                yLow = function.Compute(fit.Bias, fit.Scale, xMin);
                yHigh = function.Compute(fit.Bias, fit.Scale, xMax);
            }
            catch
            {
                // A family whose Compute throws/overflows on the observed range can't be reasoned about
                // here; defer to the power-log fallback below.
                return PowerLogSaysConstant(model, powerExponentThreshold);
            }

            if (double.IsFinite(yLow) && double.IsFinite(yHigh))
            {
                var denominator = Math.Max(Math.Max(Math.Abs(yLow), Math.Abs(yHigh)), double.Epsilon);
                var relativeSpan = Math.Abs(yHigh - yLow) / denominator;
                return relativeSpan < relativeSpanThreshold;
            }
        }

        // Fallback when measurements/parameters are unavailable: lean on the continuous exponent.
        return PowerLogSaysConstant(model, powerExponentThreshold);
    }

    private static bool PowerLogSaysConstant(ScaleFishModel model, double powerExponentThreshold)
    {
        var powerLog = model.PowerLog;
        if (powerLog is null) return false;
        if (!double.IsFinite(powerLog.B) || !double.IsFinite(powerLog.C)) return false;

        // x^b·(log x)^c ≈ constant only when both exponents are negligible.
        return Math.Abs(powerLog.B) < powerExponentThreshold
               && Math.Abs(powerLog.C) < powerExponentThreshold;
    }

    private static bool TryGetXRange(IReadOnlyList<ComplexityMeasurement>? measurements, out double xMin, out double xMax)
    {
        xMin = 0;
        xMax = 0;
        if (measurements is null || measurements.Count == 0) return false;

        var finiteXs = measurements
            .Select(m => m.X)
            .Where(x => double.IsFinite(x))
            .ToList();
        if (finiteXs.Count < 2) return false;

        xMin = finiteXs.Min();
        xMax = finiteXs.Max();
        // A degenerate range (all X equal) tells us nothing about growth.
        return xMax > xMin;
    }

    /// <summary>
    /// Builds the Warning-level hint shown for a variable whose fit looks ~O(1). Phrased as a question so a
    /// legitimately constant benchmark reads as a prompt to double-check, not a failure. Pure/string-only
    /// so the exact wording can be asserted in tests.
    /// </summary>
    public static string BuildWarningMessage(string variableName, ScaleFishModel model)
    {
        var oName = model.ScaleFishModelFunction?.OName ?? "O(1)";
        return string.Format(
            CultureInfo.InvariantCulture,
            "ScaleFish: variable '{0}' shows ~O(1) (best fit {1}) — it may not be reaching the timed work. "
            + "A common cause is building {0}-dependent state in [SailfishGlobalSetup], which runs once and is "
            + "cached/replayed across all variable sets; build it in [SailfishMethodSetup] instead. "
            + "If the work really is constant in {0}, you can ignore this.",
            variableName,
            oName);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityEstimator
{
    ScaleFishModel? EstimateComplexity(ComplexityMeasurement[] measurements);
}

public class ComplexityEstimator : IComplexityEstimator
{
    /// <summary>
    /// Default Δ-AICc threshold above which the best model is considered statistically separable from the
    /// runner-up. 2 is the standard Burnham &amp; Anderson cutoff for "some evidence". Tune via
    /// <see cref="ScaleFishSettings.DistinguishabilityDelta"/>.
    /// </summary>
    public const double DistinguishabilityDelta = 2.0;

    /// <summary>Default number of bootstrap iterations when raw replicates are available.</summary>
    public const int DefaultBootstrapIterations = 200;

    private readonly ScaleFishSettings _settings;

    /// <summary>
    /// Parameterless constructor — uses default <see cref="ScaleFishSettings"/>. Preserved for tests and
    /// callers who don't yet thread <see cref="IRunSettings"/> through their DI graph.
    /// </summary>
    public ComplexityEstimator() : this(new ScaleFishSettings())
    {
    }

    /// <summary>
    /// Uses the run's <see cref="IRunSettings.ScaleFishSettings"/> when resolved via DI.
    /// </summary>
    public ComplexityEstimator(IRunSettings runSettings) : this(runSettings?.ScaleFishSettings ?? new ScaleFishSettings())
    {
    }

    public ComplexityEstimator(ScaleFishSettings settings)
    {
        _settings = settings ?? new ScaleFishSettings();
    }

    public ScaleFishModel? EstimateComplexity(ComplexityMeasurement[] measurements)
    {
        var point = RankCandidates(measurements, _settings.DistinguishabilityDelta);
        if (point is null) return null;

        // Optional continuous-exponent diagnostic — gated by settings.
        PowerLogResult? powerLog = null;
        if (_settings.EnableContinuousExponent)
        {
            try
            {
                var weights = ScaleFishModelFunction.BuildVarianceWeights(measurements!);
                powerLog = PowerLogFit.TryFit(measurements!, weights);
            }
            catch
            {
                powerLog = null;
            }
        }

        // Optional bootstrap — only runs when raw replicates are present at every X and the user hasn't
        // opted out / set iterations to 0.
        BootstrapDiagnostic? bootstrap = null;
        if (_settings.EnableBootstrap && _settings.BootstrapIterations > 0)
        {
            bootstrap = TryBootstrap(
                measurements!,
                point.Best.Function.Name,
                _settings.BootstrapIterations,
                _settings.DistinguishabilityDelta,
                _settings.EnableParallelBootstrap);
        }

        return Build(point, powerLog, bootstrap, measurements);
    }

    private ScaleFishModel Build(PointEstimate point, PowerLogResult? powerLog, BootstrapDiagnostic? bootstrap, ComplexityMeasurement[] measurements)
    {
        return new ScaleFishModel(
            point.Best.Function,
            point.Best.Fitness.RSquared,
            point.NextBest.Function,
            point.NextBest.Fitness.RSquared,
            bestAicc: point.Best.Aicc,
            nextBestAicc: point.NextBest.Aicc,
            akaikeWeight: point.AkaikeWeight,
            isDistinguishable: point.IsDistinguishable,
            sampleSize: point.SampleSize,
            powerLog: powerLog)
        {
            Bootstrap = bootstrap,
            SuggestedNextN = SuggestNextN(point, measurements)
        };
    }

    /// <summary>
    /// When the result is not distinguishable, suggest a next X value to add. For most adjacent
    /// family ties (Linear vs NLogN, Quadratic vs Cubic, etc.) doubling the current max X gives the
    /// two candidate curves enough room to diverge to break the tie at typical noise levels.
    /// </summary>
    private static int? SuggestNextN(PointEstimate point, ComplexityMeasurement[] measurements)
    {
        if (point.IsDistinguishable) return null;
        if (measurements is null || measurements.Length == 0) return null;
        var finiteXs = measurements.Where(m => double.IsFinite(m.X) && m.X > 0).Select(m => m.X).ToArray();
        if (finiteXs.Length == 0) return null;
        var maxX = finiteXs.Max();
        var suggested = (long)Math.Ceiling(maxX * 2.0);
        if (suggested <= 0 || suggested > int.MaxValue) return null;
        return (int)suggested;
    }

    /// <summary>
    /// Fits every candidate family to the measurements and ranks them by AICc, returning the point estimate
    /// (best, runner-up, akaike weight, distinguishability). Returns null when no candidate produced a valid fit.
    /// </summary>
    internal static PointEstimate? RankCandidates(ComplexityMeasurement[]? measurements, double distinguishabilityDelta = DistinguishabilityDelta)
    {
        if (measurements is null || measurements.Length < 2) return null;

        // Filter NaN/inf observations once, upfront, so every candidate fits the same sample and the
        // AICc denominator (n) matches the residual sum it's scoring. AnalyzeFitness also invalidates
        // any family whose predictions overflow at the input X values, which keeps the effective
        // fitted sample size identical across all valid candidates.
        var cleaned = measurements.Where(m => double.IsFinite(m.Y)).ToArray();
        if (cleaned.Length < 2) return null;

        var complexityFunctions = ComplexityReferences.GetComplexityFunctions();

        var fitnessResults = new List<(ScaleFishModelFunction Function, FitnessResult Fitness)>();
        foreach (var complexityFunction in complexityFunctions)
        {
            FitnessResult fitness;
            try
            {
                fitness = complexityFunction.AnalyzeFitness(cleaned);
            }
            catch
            {
                continue;
            }

            if (fitness.IsValid)
                fitnessResults.Add((complexityFunction, fitness));
        }

        if (fitnessResults.Count == 0) return null;

        var n = cleaned.Length;
        var scored = fitnessResults
            .Select(r => new Scored(
                r.Function,
                r.Fitness,
                ComputeAicc(r.Fitness.Ssd, n, r.Function.FreeParameterCount)))
            .OrderBy(s => double.IsFinite(s.Aicc) ? s.Aicc : double.PositiveInfinity)
            .ThenBy(s => s.Fitness.Ssd)
            .ToList();

        var closest = scored[0];
        var nextClosest = scored.Count > 1 ? scored[1] : closest;

        var aiccs = scored.Select(s => s.Aicc).ToArray();
        var akaikeWeight = ComputeAkaikeWeightOfBest(aiccs);
        var delta = double.IsFinite(closest.Aicc) && double.IsFinite(nextClosest.Aicc)
            ? nextClosest.Aicc - closest.Aicc
            : double.NaN;
        var isDistinguishable = double.IsFinite(delta) && delta >= distinguishabilityDelta;

        return new PointEstimate(closest, nextClosest, akaikeWeight, isDistinguishable, n);
    }

    private static BootstrapDiagnostic? TryBootstrap(
        ComplexityMeasurement[] measurements,
        string bestFamilyName,
        int iterations,
        double distinguishabilityDelta,
        bool runInParallel)
    {
        if (measurements.Any(m => m.RawSamples is null || m.RawSamples.Length < 2))
            return null;
        if (iterations <= 0) return null;

        var baseSeed = DeterministicSeed(measurements);

        // Per-iteration result slot. Each iteration runs independently with its own RNG seeded from
        // (baseSeed, iterationIndex), so the aggregated outputs are identical whether we execute
        // serially or across many threads — `Parallel.For` is bit-for-bit equivalent to the for loop.
        var iterationResults = new IterationResult?[iterations];

        if (runInParallel && iterations > 1)
        {
            Parallel.For(0, iterations,
                i => iterationResults[i] = RunBootstrapIteration(measurements, baseSeed, i, distinguishabilityDelta));
        }
        else
        {
            for (var i = 0; i < iterations; i++)
                iterationResults[i] = RunBootstrapIteration(measurements, baseSeed, i, distinguishabilityDelta);
        }

        // Aggregate sequentially. Scale/bias samples only come from iterations whose winning family
        // matches the point estimate — those parameters live in family-specific units, so mixing
        // them across families would produce meaningless CIs.
        var scaleSamples = new List<double>(iterations);
        var biasSamples = new List<double>(iterations);
        var familyCounts = new Dictionary<string, int>();

        foreach (var result in iterationResults)
        {
            if (result is null) continue;
            familyCounts.TryGetValue(result.WinnerName, out var count);
            familyCounts[result.WinnerName] = count + 1;
            if (result.WinnerName != bestFamilyName) continue;
            scaleSamples.Add(result.Scale);
            biasSamples.Add(result.Bias);
        }

        if (familyCounts.Count == 0) return null;

        familyCounts.TryGetValue(bestFamilyName, out var agreement);
        var selectionAgreement = agreement / (double)iterations;

        var (scaleLow, scaleHigh) = Percentiles(scaleSamples, 0.025, 0.975);
        var (biasLow, biasHigh) = Percentiles(biasSamples, 0.025, 0.975);

        return new BootstrapDiagnostic(
            iterations: iterations,
            selectionAgreement: selectionAgreement,
            scaleCiLower: scaleLow,
            scaleCiUpper: scaleHigh,
            biasCiLower: biasLow,
            biasCiUpper: biasHigh);
    }

    private static IterationResult? RunBootstrapIteration(
        ComplexityMeasurement[] measurements,
        int baseSeed,
        int iterationIndex,
        double distinguishabilityDelta)
    {
        // Knuth's multiplicative hash mixed with the iteration index gives every iteration an
        // independent RNG sequence while keeping the output reproducible for identical inputs.
        // Cast to int explicitly because 2654435761 overflows int as a literal (it's uint).
        var seed = unchecked((int)(baseSeed * 2654435761u + (uint)iterationIndex));
        var rng = new Random(seed);

        var resampled = new ComplexityMeasurement[measurements.Length];
        for (var i = 0; i < measurements.Length; i++)
            resampled[i] = ResampleAtX(measurements[i], rng);

        var inner = RankCandidates(resampled, distinguishabilityDelta);
        if (inner is null) return null;

        var winner = inner.Best.Function;
        if (winner.FunctionParameters is null)
            return new IterationResult(winner.Name, double.NaN, double.NaN);

        return new IterationResult(winner.Name, winner.FunctionParameters.Scale, winner.FunctionParameters.Bias);
    }

    private static ComplexityMeasurement ResampleAtX(ComplexityMeasurement source, Random rng)
    {
        var raw = source.RawSamples!;
        var resampled = new double[raw.Length];
        double sum = 0;
        for (var i = 0; i < raw.Length; i++)
        {
            var pick = raw[rng.Next(raw.Length)];
            resampled[i] = pick;
            sum += pick;
        }
        var mean = sum / raw.Length;
        double sqSum = 0;
        for (var i = 0; i < raw.Length; i++)
        {
            var d = resampled[i] - mean;
            sqSum += d * d;
        }
        var stdDev = raw.Length > 1 ? Math.Sqrt(sqSum / (raw.Length - 1)) : 0.0;
        return new ComplexityMeasurement(source.X, mean, stdDev, raw.Length, resampled);
    }

    private sealed record IterationResult(string WinnerName, double Scale, double Bias);

    private static (double lower, double upper) Percentiles(List<double> samples, double low, double high)
    {
        if (samples.Count == 0) return (double.NaN, double.NaN);
        var sorted = samples.OrderBy(v => v).ToArray();
        return (PercentileFromSorted(sorted, low), PercentileFromSorted(sorted, high));
    }

    private static double PercentileFromSorted(double[] sorted, double p)
    {
        if (sorted.Length == 0) return double.NaN;
        if (sorted.Length == 1) return sorted[0];
        var rank = p * (sorted.Length - 1);
        var lo = (int)Math.Floor(rank);
        var hi = (int)Math.Ceiling(rank);
        if (lo == hi) return sorted[lo];
        var frac = rank - lo;
        return sorted[lo] * (1 - frac) + sorted[hi] * frac;
    }

    /// <summary>
    /// Stable seed derived from the X/Y signature so bootstrap output is repeatable for identical inputs.
    /// </summary>
    private static int DeterministicSeed(ComplexityMeasurement[] measurements)
    {
        unchecked
        {
            var hash = 17;
            foreach (var m in measurements)
            {
                hash = hash * 31 + m.X.GetHashCode();
                hash = hash * 31 + m.Y.GetHashCode();
                hash = hash * 31 + m.SampleSize.GetHashCode();
            }
            return hash;
        }
    }

    /// <summary>
    /// AIC with small-sample correction for OLS. n is the number of measurements, k the number of free
    /// parameters (typically 2: scale, bias). Returns +infinity when RSS or n is degenerate.
    /// </summary>
    internal static double ComputeAicc(double rss, int n, int k)
    {
        if (n <= 0 || !double.IsFinite(rss) || rss < 0) return double.PositiveInfinity;
        var safeRss = Math.Max(rss, 1e-300);
        var aic = n * Math.Log(safeRss / n) + 2.0 * k;

        var denom = n - k - 1;
        if (denom <= 0) return double.PositiveInfinity;
        return aic + 2.0 * k * (k + 1) / denom;
    }

    internal static double ComputeAkaikeWeightOfBest(double[] aiccValues)
    {
        if (aiccValues.Length == 0) return double.NaN;
        var finite = aiccValues.Where(double.IsFinite).ToArray();
        if (finite.Length == 0) return double.NaN;

        var min = finite.Min();
        double sum = 0;
        foreach (var v in finite) sum += Math.Exp(-0.5 * (v - min));
        if (sum <= 0 || !double.IsFinite(sum)) return double.NaN;
        return 1.0 / sum;
    }

    internal record Scored(ScaleFishModelFunction Function, FitnessResult Fitness, double Aicc);

    internal record PointEstimate(Scored Best, Scored NextBest, double AkaikeWeight, bool IsDistinguishable, int SampleSize);
}

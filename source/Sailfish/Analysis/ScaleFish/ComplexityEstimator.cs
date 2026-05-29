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

        // Optional cross-validation — leave-one-X-out check against the all-data classification.
        CrossValidationDiagnostic? crossValidation = null;
        if (_settings.EnableCrossValidation)
        {
            crossValidation = RunCrossValidation(
                measurements!,
                point.Best.Function.Name,
                _settings.DistinguishabilityDelta);
        }

        // Optional per-percentile fits — needs raw replicates at every X.
        IReadOnlyList<TailFitResult> tailFits = Array.Empty<TailFitResult>();
        if (_settings.EnableTailPercentileFits
            && _settings.TailPercentiles is { Length: > 0 }
            && measurements!.All(m => m.RawSamples is { Length: >= 2 }))
        {
            tailFits = RunTailPercentileFits(measurements!, _settings.TailPercentiles, _settings.DistinguishabilityDelta);
        }

        return Build(point, powerLog, bootstrap, crossValidation, tailFits, measurements);
    }

    private ScaleFishModel Build(
        PointEstimate point,
        PowerLogResult? powerLog,
        BootstrapDiagnostic? bootstrap,
        CrossValidationDiagnostic? crossValidation,
        IReadOnlyList<TailFitResult> tailFits,
        ComplexityMeasurement[] measurements)
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
            CrossValidation = crossValidation,
            TailFits = tailFits,
            SuggestedNextN = SuggestNextN(point, measurements)
        };
    }

    /// <summary>
    /// For each requested percentile, replace each X's mean with that percentile of its raw replicates
    /// and re-run the family-ranking pipeline. Produces a slim per-percentile classification so the
    /// caller can see whether the tail (p95/p99) scales differently from the mean.
    /// </summary>
    private static IReadOnlyList<TailFitResult> RunTailPercentileFits(
        ComplexityMeasurement[] measurements,
        double[] percentiles,
        double distinguishabilityDelta)
    {
        var results = new List<TailFitResult>(percentiles.Length);
        foreach (var p in percentiles)
        {
            if (!(p > 0 && p < 1)) continue;
            var shaped = new ComplexityMeasurement[measurements.Length];
            for (var i = 0; i < measurements.Length; i++)
            {
                var raw = measurements[i].RawSamples!;
                var y = PercentileOf(raw, p);
                // Slim measurement — y replaced with the percentile, retain X. Drop raw samples to
                // keep the tail-fit recursion bounded (no nested bootstrap on tail fits).
                shaped[i] = new ComplexityMeasurement(measurements[i].X, y);
            }

            var foldPoint = RankCandidates(shaped, distinguishabilityDelta);
            if (foldPoint is null) continue;

            var best = foldPoint.Best.Function;
            var next = foldPoint.NextBest.Function;
            var bestScale = best.FunctionParameters?.Scale ?? double.NaN;
            var bestBias = best.FunctionParameters?.Bias ?? double.NaN;

            results.Add(new TailFitResult(
                percentile: p,
                bestFamilyName: best.Name,
                bestFamilyOName: best.OName,
                bestRSquared: foldPoint.Best.Fitness.RSquared,
                nextFamilyName: next.Name,
                nextRSquared: foldPoint.NextBest.Fitness.RSquared,
                bestAicc: foldPoint.Best.Aicc,
                nextBestAicc: foldPoint.NextBest.Aicc,
                akaikeWeight: foldPoint.AkaikeWeight,
                isDistinguishable: foldPoint.IsDistinguishable,
                sampleSize: foldPoint.SampleSize,
                bestScale: bestScale,
                bestBias: bestBias));
        }
        return results;
    }

    /// <summary>
    /// Linear-interpolated percentile of an unsorted sample. <c>p</c> is in (0, 1).
    /// </summary>
    private static double PercentileOf(double[] samples, double p)
    {
        if (samples.Length == 0) return double.NaN;
        if (samples.Length == 1) return samples[0];
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var rank = p * (sorted.Length - 1);
        var lo = (int)Math.Floor(rank);
        var hi = (int)Math.Ceiling(rank);
        if (lo == hi) return sorted[lo];
        var frac = rank - lo;
        return sorted[lo] * (1 - frac) + sorted[hi] * frac;
    }

    /// <summary>
    /// Leave-one-X-out cross-validation. For each X, refit all candidates on the remaining points, record
    /// whether the fold's best matches the all-data best (rank agreement), and predict at the held-out X
    /// using the all-data winning family refit on the fold's training data (mean/median prediction error).
    /// </summary>
    private static CrossValidationDiagnostic? RunCrossValidation(
        ComplexityMeasurement[] measurements,
        string allDataBestFamilyName,
        double distinguishabilityDelta)
    {
        // Need at least 4 X values so each fold trains on >= 3 (the minimum n for the OLS fit to be well-posed).
        if (measurements is null || measurements.Length < 4) return null;

        // Pre-filter the same way RankCandidates would, so n-counts align.
        var cleaned = measurements.Where(m => double.IsFinite(m.Y) && double.IsFinite(m.X)).ToArray();
        if (cleaned.Length < 4) return null;

        var predErrors = new List<double>(cleaned.Length);
        var agreements = 0;
        var validFolds = 0;

        for (var i = 0; i < cleaned.Length; i++)
        {
            var heldOut = cleaned[i];
            var trainCount = cleaned.Length - 1;
            var train = new ComplexityMeasurement[trainCount];
            for (int src = 0, dst = 0; src < cleaned.Length; src++)
            {
                if (src == i) continue;
                train[dst++] = cleaned[src];
            }

            var foldPoint = RankCandidates(train, distinguishabilityDelta);
            if (foldPoint is null) continue;

            if (string.Equals(foldPoint.Best.Function.Name, allDataBestFamilyName, StringComparison.Ordinal))
                agreements++;

            // Refit the all-data winning family on this fold's training data to get an honest prediction.
            var ownInstance = ComplexityFunctionRegistry.CreateFitInstances()
                .FirstOrDefault(f => f.Name == allDataBestFamilyName);
            if (ownInstance is null) continue;

            try
            {
                var fit = ownInstance.AnalyzeFitness(train);
                if (!fit.IsValid || ownInstance.FunctionParameters is null) continue;
                var predicted = ownInstance.Predict((int)heldOut.X);
                if (!double.IsFinite(predicted)) continue;
                var err = Math.Abs(predicted - heldOut.Y);
                if (!double.IsFinite(err)) continue;
                predErrors.Add(err);
                validFolds++;
            }
            catch
            {
                // skip this fold
            }
        }

        // Require at least 3 valid folds to report a CV signal — fewer is statistically meaningless.
        if (validFolds < 3 || predErrors.Count < 3) return null;

        var rankAgreement = agreements / (double)validFolds;
        var mean = predErrors.Average();
        var sorted = predErrors.OrderBy(v => v).ToArray();
        double median;
        if (sorted.Length % 2 == 0)
            median = (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0;
        else
            median = sorted[sorted.Length / 2];

        return new CrossValidationDiagnostic(validFolds, rankAgreement, mean, median);
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

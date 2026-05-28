using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityEstimator
{
    ScaleFishModel? EstimateComplexity(ComplexityMeasurement[] measurements);
}

public class ComplexityEstimator : IComplexityEstimator
{
    /// <summary>
    /// Δ-AICc threshold above which the best model is considered statistically separable from the runner-up.
    /// 2 is the standard Burnham &amp; Anderson cutoff for "some evidence"; we use it as the distinguishability gate.
    /// </summary>
    public const double DistinguishabilityDelta = 2.0;

    /// <summary>Default number of bootstrap iterations when raw replicates are available.</summary>
    public const int DefaultBootstrapIterations = 200;

    public ScaleFishModel? EstimateComplexity(ComplexityMeasurement[] measurements)
    {
        var point = RankCandidates(measurements);
        if (point is null) return null;

        // Optional continuous-exponent diagnostic
        PowerLogResult? powerLog;
        try
        {
            var weights = ScaleFishModelFunction.BuildVarianceWeights(measurements!);
            powerLog = PowerLogFit.TryFit(measurements!, weights);
        }
        catch
        {
            powerLog = null;
        }

        // Optional bootstrap — only runs when raw replicates are present at every X
        var bootstrap = TryBootstrap(measurements!, point.Best.Function.Name, point.Best.Function.FunctionParameters);

        return Build(point, powerLog, bootstrap);
    }

    private static ScaleFishModel Build(PointEstimate point, PowerLogResult? powerLog, BootstrapDiagnostic? bootstrap)
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
            Bootstrap = bootstrap
        };
    }

    /// <summary>
    /// Fits every candidate family to the measurements and ranks them by AICc, returning the point estimate
    /// (best, runner-up, akaike weight, distinguishability). Returns null when no candidate produced a valid fit.
    /// </summary>
    internal static PointEstimate? RankCandidates(ComplexityMeasurement[]? measurements)
    {
        if (measurements is null || measurements.Length < 2) return null;
        var complexityFunctions = ComplexityReferences.GetComplexityFunctions();

        var fitnessResults = new List<(ScaleFishModelFunction Function, FitnessResult Fitness)>();
        foreach (var complexityFunction in complexityFunctions)
        {
            FitnessResult fitness;
            try
            {
                fitness = complexityFunction.AnalyzeFitness(measurements);
            }
            catch
            {
                continue;
            }

            if (fitness.IsValid)
                fitnessResults.Add((complexityFunction, fitness));
        }

        if (fitnessResults.Count == 0) return null;

        var n = measurements.Length;
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
        var isDistinguishable = double.IsFinite(delta) && delta >= DistinguishabilityDelta;

        return new PointEstimate(closest, nextClosest, akaikeWeight, isDistinguishable, n);
    }

    private static BootstrapDiagnostic? TryBootstrap(
        ComplexityMeasurement[] measurements,
        string bestFamilyName,
        FittedCurve? pointParameters)
    {
        if (measurements.Any(m => m.RawSamples is null || m.RawSamples.Length < 2))
            return null;

        var iterations = DefaultBootstrapIterations;
        var rng = new Random(DeterministicSeed(measurements));

        var scaleSamples = new List<double>(iterations);
        var biasSamples = new List<double>(iterations);
        var familyCounts = new Dictionary<string, int>();

        for (var iter = 0; iter < iterations; iter++)
        {
            var resampled = new ComplexityMeasurement[measurements.Length];
            for (var i = 0; i < measurements.Length; i++)
            {
                resampled[i] = ResampleAtX(measurements[i], rng);
            }

            var inner = RankCandidates(resampled);
            if (inner is null) continue;

            var winner = inner.Best.Function;
            familyCounts.TryGetValue(winner.Name, out var count);
            familyCounts[winner.Name] = count + 1;

            if (winner.FunctionParameters is null) continue;
            scaleSamples.Add(winner.FunctionParameters.Scale);
            biasSamples.Add(winner.FunctionParameters.Bias);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Analysis.ScaleFish;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Shared helpers for generating synthetic ScaleFish measurements with controlled noise.
/// Random sources are caller-seeded so all tests using these helpers are deterministic.
/// </summary>
internal static class ScaleFishTestHelpers
{
    /// <summary>
    /// Produces a generic complexity-function instance by reflection so tests can iterate over
    /// every family without manually listing them.
    /// </summary>
    public static ScaleFishModelFunction CreateFamily<T>() where T : ScaleFishModelFunction
    {
        var ctor = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        return (ScaleFishModelFunction)ctor.Invoke([]);
    }

    /// <summary>
    /// Noise-free synthetic measurements: y_i = function(x_i) with scale=1, bias=0.
    /// </summary>
    public static ComplexityMeasurement[] BuildExact(
        ScaleFishModelFunction function,
        IEnumerable<int> xs)
    {
        return xs
            .Select(x => new ComplexityMeasurement(x, function.Compute(0.0, 1.0, x)))
            .ToArray();
    }

    /// <summary>
    /// Synthetic measurements with multiplicative Gaussian noise on individual replicates,
    /// then the mean/sd/raw samples assembled in the same shape Sailfish would produce at runtime.
    /// Use a small <paramref name="relativeNoise"/> (e.g. 0.05) to mimic well-warmed benchmarks.
    /// </summary>
    public static ComplexityMeasurement[] BuildNoisy(
        Func<double, double> trueFunction,
        IEnumerable<int> xs,
        int sampleSize,
        double relativeNoise,
        Random rng)
    {
        if (sampleSize <= 0) throw new ArgumentException("sampleSize must be > 0", nameof(sampleSize));
        if (relativeNoise < 0) throw new ArgumentException("relativeNoise must be ≥ 0", nameof(relativeNoise));
        if (rng is null) throw new ArgumentNullException(nameof(rng));

        return xs
            .Select(x =>
            {
                var trueMean = trueFunction(x);
                var samples = new double[sampleSize];
                for (var i = 0; i < sampleSize; i++)
                {
                    var z = NextGaussian(rng);
                    samples[i] = trueMean * (1.0 + relativeNoise * z);
                }
                var mean = samples.Average();
                var n = samples.Length;
                double sqSum = 0;
                for (var i = 0; i < n; i++)
                {
                    var d = samples[i] - mean;
                    sqSum += d * d;
                }
                var stdDev = n > 1 ? Math.Sqrt(sqSum / (n - 1)) : 0.0;
                return new ComplexityMeasurement(x, mean, stdDev, n, samples);
            })
            .ToArray();
    }

    /// <summary>
    /// Geometric spacing of integer X values between <paramref name="min"/> and <paramref name="max"/>.
    /// </summary>
    public static int[] LogSpacedX(int min, int max, int count)
    {
        if (count < 2) throw new ArgumentException("count must be ≥ 2");
        if (min < 1) throw new ArgumentException("min must be ≥ 1");
        if (max <= min) throw new ArgumentException("max must be > min");
        // Need at least `count` distinct integers in [min, max].
        if (count > max - min + 1)
            throw new ArgumentException(
                $"Cannot generate {count} distinct geometrically-spaced integers in [{min}, {max}]");

        var ratio = Math.Pow((double)max / min, 1.0 / (count - 1));
        var result = new int[count];
        var previous = int.MinValue;
        for (var i = 0; i < count; i++)
        {
            var v = (int)Math.Round(min * Math.Pow(ratio, i));
            // Force strict increase if rounding collapses adjacent points, but never exceed max.
            // The feasibility check above guarantees we have room to bump within [min, max].
            if (v <= previous) v = previous + 1;
            if (v > max) v = max;
            previous = v;
            result[i] = v;
        }
        return result;
    }

    /// <summary>
    /// Box-Muller sample from a standard normal distribution.
    /// </summary>
    public static double NextGaussian(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}

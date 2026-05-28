using System;

namespace Sailfish.Analysis.ScaleFish;

public class ComplexityMeasurement
{
    public ComplexityMeasurement(double x, double y)
        : this(x, y, stdDev: 0.0, sampleSize: 1, rawSamples: null)
    {
    }

    public ComplexityMeasurement(double x, double y, double stdDev, int sampleSize, double[]? rawSamples = null)
    {
        X = x;
        Y = y;
        StdDev = stdDev;
        SampleSize = sampleSize <= 0 ? 1 : sampleSize;
        RawSamples = rawSamples;
    }

    /// <summary>
    ///     An integer variable that represents a scale for some aspect of your system. 1 record in the database, 10 elements
    ///     in a thing
    /// </summary>
    public double X { get; set; }

    /// <summary>
    ///     A double that represents the resulting time measurement for the given X (typically the mean of replicates).
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    ///     Standard deviation of replicate measurements at this X. 0 when no replicate information is available.
    /// </summary>
    public double StdDev { get; set; }

    /// <summary>
    ///     Number of replicate measurements that produced <see cref="Y"/>. Defaults to 1 when unknown.
    /// </summary>
    public int SampleSize { get; set; }

    /// <summary>
    ///     Optional raw replicate samples used to compute <see cref="Y"/>. Enables bootstrap-style uncertainty quantification.
    ///     Null when not available.
    /// </summary>
    public double[]? RawSamples { get; set; }

    /// <summary>
    ///     Standard error of the mean at this X (StdDev / sqrt(SampleSize)). Returns 0 when no replicate info is present.
    /// </summary>
    public double StandardError => SampleSize > 1 && StdDev > 0
        ? StdDev / Math.Sqrt(SampleSize)
        : 0.0;

    /// <summary>
    ///     True when this measurement carries usable replicate uncertainty information.
    /// </summary>
    public bool HasUncertainty => SampleSize > 1 && StdDev > 0;
}

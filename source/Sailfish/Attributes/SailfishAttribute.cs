using System;
using System.ComponentModel.DataAnnotations;
using Sailfish.Analysis;


namespace Sailfish.Attributes;

/// <summary>
///     Specifies that a class is a Sailfish test class.
/// </summary>
/// <seealso cref="SailfishMethodAttribute" />
/// <seealso cref="SailfishIterationSetupAttribute" />
/// <seealso cref="SailfishIterationTeardownAttribute" />
/// <seealso cref="SailfishGlobalSetupAttribute" />
/// <seealso cref="SailfishGlobalTeardownAttribute" />
/// <seealso cref="SailfishVariableAttribute" />
/// <seealso cref="SuppressConsoleAttribute" />
/// <seealso cref="WriteToCsvAttribute" />
/// <seealso cref="WriteToMarkdownAttribute" />
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/the-sailfish-attribute">The Sailfish Attributes</seealso>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SailfishAttribute : Attribute
{
    private const int DefaultNumIterations = 3;
    private const int DefaultNumWarmupIterations = 3;

    internal SailfishAttribute()
    {
        SampleSize = DefaultNumIterations;
        NumWarmupIterations = DefaultNumWarmupIterations;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SailfishAttribute" /> class with the specified number of iterations
    ///     and warm-up iterations.
    /// </summary>
    /// <param name="sampleSize">The number of times each SailfishMethod will be iterated.</param>
    /// <param name="numWarmupIterations">
    ///     The number of times each SailfishMethod will be iterated without being timed before
    ///     executing <paramref name="sampleSize" /> with tracking.
    /// </param>
    /// <remarks>
    ///     Each iteration includes the invocation of three methods: SailfishIterationSetup, SailfishMethod, and
    ///     SailfishIterationTeardown, in that order.
    /// </remarks>
    public SailfishAttribute(
        [Range(2, int.MaxValue)] int sampleSize = DefaultNumIterations,
        [Range(0, int.MaxValue)] int numWarmupIterations = DefaultNumWarmupIterations)
    {
        SampleSize = sampleSize;
        NumWarmupIterations = numWarmupIterations;
    }

    /// <summary>
    ///     Gets or sets the number of iterations for each SailfishMethod.
    /// </summary>
    /// <value>The number of iterations.</value>
    [Range(2, int.MaxValue)]
    public int SampleSize { get; set; }

    /// <summary>
    ///     Gets or sets the number of warm-up iterations for each SailfishMethod.
    /// </summary>
    /// <value>The number of warm-up iterations.</value>
    [Range(0, int.MaxValue)]
    public int NumWarmupIterations { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the Sailfish test is disabled.
    /// </summary>
    /// <value><c>true</c> if the test is disabled; otherwise, <c>false</c>.</value>
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets/Sets whether to disable overhead estimation for the entire class.
    /// </summary>
    public bool DisableOverheadEstimation { get; set; }

    /// <summary>
    ///     Gets or sets whether to use adaptive sampling for this test class.
    ///     When enabled, tests will continue until statistical convergence is achieved.
    /// </summary>
    public bool UseAdaptiveSampling { get; set; } = false;

    /// <summary>
    ///     Gets or sets the target coefficient of variation for convergence detection.
    ///     Lower values require more statistical precision. Default is 0.05 (5%).
    /// </summary>
    public double TargetCoefficientOfVariation { get; set; } = 0.05;

    /// <summary>
    ///     Gets or sets the maximum number of samples when using adaptive sampling.
    ///     Prevents infinite loops in case of non-converging tests. Default is 1000.
    /// </summary>
    public int MaximumSampleSize { get; set; } = 1000;

    // --- Additional execution/statistics knobs (opt-in at class level) ---

    /// <summary>
    /// Minimum sample size when using adaptive sampling (default 10).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MinimumSampleSize { get; set; } = 10;

    /// <summary>
    /// Confidence level used for interval estimation (default 0.95 => 95%).
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Maximum allowed confidence interval width. Interpreted as relative width when UseRelativeConfidenceInterval=true.
    /// Default 0.20 (20%).
    /// </summary>
    public double MaxConfidenceIntervalWidth { get; set; } = 0.20;

    /// <summary>
    /// If true (default), MaxConfidenceIntervalWidth is interpreted as a relative width (e.g., 20%).
    /// </summary>
    public bool UseRelativeConfidenceInterval { get; set; } = true;

    /// <summary>
    /// Number of operations invoked per measured iteration. Default 1. Used to amortize timer overhead for microbenchmarks.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int OperationsPerInvoke { get; set; } = 1;

    /// <summary>
    /// Target duration per measured iteration in milliseconds (0 disables targeting). Useful for auto-tuning OperationsPerInvoke.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TargetIterationDurationMs { get; set; } = 0;

    /// <summary>
    /// Maximum measurement time per method in milliseconds (0 disables limit).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MaxMeasurementTimePerMethodMs { get; set; } = 0;

    /// <summary>
    /// Enable precision/time budget controller that relaxes precision targets slightly to fit the per-method time budget. Default false.
    /// </summary>
    public bool UseTimeBudgetController { get; set; } = false;


    /// <summary>
    /// Enable lightweight default diagnosers for this class (memory/GC/threading). Default false.
    /// </summary>
    public bool EnableDefaultDiagnosers { get; set; } = false;

    /// <summary>
    /// Preferred outlier handling strategy when configurable detection is enabled.
    /// Default is RemoveUpper to preserve typical performance-testing semantics.
    /// </summary>
    public OutlierStrategy OutlierStrategy { get; set; } = OutlierStrategy.RemoveUpper;

    /// <summary>
    /// Opt-in to settings-driven outlier handling for this class. When false (default),
    /// the legacy SailfishOutlierDetector path is used to preserve backward compatibility.
    /// </summary>
    public bool UseConfigurableOutlierDetection { get; set; } = false;

}
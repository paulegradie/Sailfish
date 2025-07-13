using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Attributes;

/// <summary>
/// Marks methods for direct performance comparison within the same test run.
/// Methods with the same ComparisonGroup will be executed and compared statistically.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SailfishMethodComparisonAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the SailfishMethodComparisonAttribute class.
    /// </summary>
    /// <param name="comparisonGroup">The name of the comparison group. Methods with the same group name will be compared.</param>
    public SailfishMethodComparisonAttribute(string comparisonGroup)
    {
        ComparisonGroup = comparisonGroup ?? throw new ArgumentNullException(nameof(comparisonGroup));
    }

    /// <summary>
    /// The name of the comparison group for statistical analysis.
    /// </summary>
    [Required]
    public string ComparisonGroup { get; }

    /// <summary>
    /// Optional baseline method name. If specified, all comparisons will be relative to this method.
    /// </summary>
    public string? BaselineMethod { get; set; }

    /// <summary>
    /// Statistical significance level for comparisons (default: 0.05).
    /// </summary>
    [Range(0.001, 0.1)]
    public double SignificanceLevel { get; set; } = 0.05;

    /// <summary>
    /// Whether to include this method in the comparison output (default: true).
    /// </summary>
    public bool IncludeInComparison { get; set; } = true;
}

using System;

namespace Sailfish.Attributes;

/// <summary>
/// Specifies that a method should be included in performance comparisons.
/// </summary>
/// <remarks>
/// This attribute is obsolete. Comparison configuration has been folded into
/// <see cref="SailfishMethodAttribute"/>:
///
/// <code>
/// // Before:
/// [SailfishMethod]
/// [SailfishComparison("Sort")]
/// public void QuickSort() { /* ... */ }
///
/// // After:
/// [SailfishMethod(ComparisonGroup = "Sort", IsBaseline = true)]
/// public void QuickSort() { /* ... */ }
/// </code>
///
/// During the deprecation window, this attribute is still honoured by the runtime as a fallback,
/// but the analyzer will warn at build time and the type will be removed in the next major release.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
[Obsolete(
    "Use SailfishMethod(ComparisonGroup = \"...\", IsBaseline = true|false) instead. " +
    "SailfishComparisonAttribute will be removed in the next major release.",
    error: false)]
public sealed class SailfishComparisonAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SailfishComparisonAttribute"/> class.
    /// </summary>
    /// <param name="comparisonGroup">The name of the comparison group. Methods with the same group name will be compared.</param>
    public SailfishComparisonAttribute(string comparisonGroup)
    {
        if (string.IsNullOrWhiteSpace(comparisonGroup))
            throw new ArgumentException("Comparison group cannot be null or empty.", nameof(comparisonGroup));

        ComparisonGroup = comparisonGroup;
    }

    /// <summary>
    /// Gets the name of the comparison group.
    /// Methods with the same group name will be compared against each other.
    /// </summary>
    public string ComparisonGroup { get; }

    /// <summary>
    /// Gets or sets the display name for this method in comparison results.
    /// If not specified, the method name will be used.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether this comparison should be disabled.
    /// When disabled, the method will not participate in comparisons even if other methods in the group are present.
    /// </summary>
    public bool Disabled { get; set; }
}

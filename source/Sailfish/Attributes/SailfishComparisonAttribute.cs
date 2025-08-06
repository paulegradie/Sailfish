using System;

namespace Sailfish.Attributes;

/// <summary>
/// Specifies that a method should be included in performance comparisons.
/// Methods with the same comparison group will be compared against each other
/// when the full test class is executed.
/// </summary>
/// <remarks>
/// This attribute enables performance comparisons between methods in the same test class.
/// When individual methods are run, only their results are shown with comparison info.
/// When the full class is run, statistical comparisons are performed between methods
/// in the same comparison group and displayed in the test output.
///
/// Example usage:
/// <code>
/// [SailfishComparison("SortingAlgorithms")]
/// public void BubbleSort() { /* implementation */ }
///
/// [SailfishComparison("SortingAlgorithms")]
/// public void QuickSort() { /* implementation */ }
///
/// [SailfishComparison("SortingAlgorithms")]
/// public void MergeSort() { /* implementation */ }
/// </code>
///
/// This creates an N×N comparison matrix between all methods in the group.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
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



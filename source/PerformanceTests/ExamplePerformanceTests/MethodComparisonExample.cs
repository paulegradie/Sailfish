using Sailfish.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example test class demonstrating the method comparison feature.
/// Comparison configuration lives on <see cref="SailfishMethodAttribute"/> itself:
/// set <c>ComparisonGroup</c> to opt a method into a named comparison, and optionally
/// set <c>IsBaseline = true</c> on one method per group to switch from the default N×N
/// matrix to N−1 baseline-vs-contender comparisons.
/// </summary>
/// <remarks>
/// - Run individual methods: only that method's results are displayed.
/// - Run the whole class: comparisons are emitted to the consolidated markdown and CSV.
///
/// This example exercises both modes:
/// - <c>SumCalculation</c> has no baseline → full N×N matrix is rendered.
/// - <c>SortingAlgorithm</c> nominates <c>SortWithQuickSort</c> as the baseline → the
///   table reports each contender's ratio vs. quicksort (N−1 rows).
/// </remarks>
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(DisableOverheadEstimation = true, SampleSize = 100)]
public class MethodComparisonExample
{
    private readonly List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup()
    {
        // Initialize test data
        _data.Clear();
        for (var i = 0; i < 1000; i++)
        {
            _data.Add(i);
        }
    }

    /// <summary>
    /// LINQ-based sum calculation algorithm. Part of the SumCalculation group (no baseline → N×N).
    /// </summary>
    [SailfishMethod(ComparisonGroup = "SumCalculation")]
    public void CalculateSumWithLinq()
    {
        var sum = _data.Sum();
        // Simulate some work
        Thread.Sleep(1);
    }

    /// <summary>
    /// Loop-based sum calculation algorithm. Part of the SumCalculation group (no baseline → N×N).
    /// </summary>
    [SailfishMethod(ComparisonGroup = "SumCalculation")]
    public void CalculateSumWithLoop()
    {
        var sum = 0;
        for (var i = 0; i < _data.Count; i++)
        {
            sum += _data[i];
        }
        // Simulate some work
        Thread.Sleep(1);
    }

    /// <summary>
    /// Bubble sort algorithm — contender in the SortingAlgorithm group.
    /// </summary>
    [SailfishMethod(ComparisonGroup = "SortingAlgorithm")]
    public void SortWithBubbleSort()
    {
        var array = _data.ToArray();

        // Simple bubble sort implementation
        for (var i = 0; i < array.Length - 1; i++)
        {
            for (var j = 0; j < array.Length - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }

    /// <summary>
    /// Another contender — kept slow on purpose to show non-baseline contender behavior.
    /// </summary>
    [SailfishMethod(ComparisonGroup = "SortingAlgorithm")]
    public void SortWithOtherSort()
    {
        Thread.Sleep(10);
    }

    /// <summary>
    /// Optimized sorting algorithm — declared as the baseline of SortingAlgorithm.
    /// All other methods in this group are reported as ratios vs. this one.
    /// </summary>
    [SailfishMethod(ComparisonGroup = "SortingAlgorithm", IsBaseline = true)]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array); // Using built-in optimized sort
    }

    /// <summary>
    /// Regular method without comparison — will not participate in any comparison group.
    /// </summary>
    [SailfishMethod]
    public void RegularMethod()
    {
        var count = _data.Count;
        Thread.Sleep(1);
    }

    /// <summary>
    /// Another regular method — demonstrates that non-comparison methods coexist with comparison ones.
    /// </summary>
    [SailfishMethod]
    public void AnotherRegularMethod()
    {
        var first = _data.FirstOrDefault();
        Thread.Sleep(1);
    }
}

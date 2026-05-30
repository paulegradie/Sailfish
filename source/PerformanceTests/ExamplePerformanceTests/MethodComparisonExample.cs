using Sailfish.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example test class demonstrating Sailfish's class-default method comparison.
/// </summary>
/// <remarks>
/// Every <c>[SailfishMethod]</c> in a <c>[Sailfish]</c> class is automatically a member of the
/// implicit class-wide comparison group — no extra attributes needed. When one method sets
/// <c>IsBaseline = true</c>, the output is a baseline-vs-contender table (N−1 comparisons);
/// when none do, the output is an N×N matrix.
///
/// Here <c>QuickSort</c> is declared the baseline; the bubble-sort and Array.Sort contenders are
/// reported as ratios vs. quicksort.
///
/// - <see cref="MultiGroupComparisonExample"/> shows the advanced path: multiple explicit
///   comparison groups in one class via <c>[SailfishMethod(ComparisonGroup = "...")]</c>.
/// - <see cref="DisabledComparisonExample"/> shows opting a class out of comparison entirely.
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
        _data.Clear();
        for (var i = 0; i < 1000; i++)
        {
            _data.Add(i);
        }
    }

    /// <summary>
    /// Optimized sort — declared the baseline. Every other method in the class is reported
    /// as a ratio vs. this one in the comparison output.
    /// </summary>
    [SailfishMethod(IsBaseline = true)]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

    /// <summary>
    /// Bubble sort — a contender. Will appear in the comparison table as a ratio vs. QuickSort.
    /// </summary>
    [SailfishMethod]
    public void SortWithBubbleSort()
    {
        var array = _data.ToArray();
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
    /// Deliberately slow contender — exists to make the comparison output visually obvious.
    /// </summary>
    [SailfishMethod]
    public void SortWithSleepyPlaceholder()
    {
        Thread.Sleep(10);
    }
}

using Sailfish.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example test class demonstrating the new method comparison feature.
/// This class shows how to use SailfishComparisonAttribute to mark methods
/// for "before and after" performance comparisons.
/// </summary>
/// <remarks>
/// When running individual methods, only that method's results are displayed.
/// When running the entire class, SailDiff comparisons are performed between
/// methods in the same comparison group and displayed in the test output.
/// 
/// Usage:
/// - Run individual methods: Only method results shown
/// - Run entire class: Method results + SailDiff comparison results shown
/// </remarks>
[WriteToMarkdown]
[Sailfish(DisableOverheadEstimation = true, SampleSize = 100)]
public class MethodComparisonExample
{
    private readonly List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup()
    {
        // Initialize test data
        _data.Clear();
        for (int i = 0; i < 1000; i++)
        {
            _data.Add(i);
        }
    }

    /// <summary>
    /// LINQ-based sum calculation algorithm.
    /// </summary>
    [SailfishMethod]
    [SailfishComparison("SumCalculation")]
    public void CalculateSumWithLinq()
    {
        var sum = _data.Sum();
        // Simulate some work
        Thread.Sleep(1);
    }

    /// <summary>
    /// Loop-based sum calculation algorithm.
    /// </summary>
    [SailfishMethod]
    [SailfishComparison("SumCalculation")]
    public void CalculateSumWithLoop()
    {
        var sum = 0;
        for (int i = 0; i < _data.Count; i++)
        {
            sum += _data[i];
        }
        // Simulate some work
        Thread.Sleep(1);
    }

    /// <summary>
    /// Bubble sort algorithm implementation.
    /// </summary>
    [SailfishMethod]
    [SailfishComparison("SortingAlgorithm")]
    public void SortWithBubbleSort()
    {
        var array = _data.ToArray();

        // Simple bubble sort implementation
        for (int i = 0; i < array.Length - 1; i++)
        {
            for (int j = 0; j < array.Length - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }

    [SailfishMethod]
    [SailfishComparison("SortingAlgorithm")]
    public void SortWithOtherSort()
    {
        // Simple bubble sort implementation
        Thread.Sleep(10);
    }
    
    
    /// <summary>
    /// Optimized sorting algorithm using Array.Sort.
    /// </summary>
    [SailfishMethod]
    [SailfishComparison("SortingAlgorithm")]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array); // Using built-in optimized sort
    }

    /// <summary>
    /// Regular method without comparison - will not participate in comparisons.
    /// </summary>
    [SailfishMethod]
    public void RegularMethod()
    {
        var count = _data.Count;
        Thread.Sleep(1);
    }

    /// <summary>
    /// Another regular method - demonstrates that non-comparison methods work normally.
    /// </summary>
    [SailfishMethod]
    public void AnotherRegularMethod()
    {
        var first = _data.FirstOrDefault();
        Thread.Sleep(1);
    }
}

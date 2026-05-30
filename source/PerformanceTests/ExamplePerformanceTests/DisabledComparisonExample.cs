using Sailfish.Attributes;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example: opt a class out of method comparison entirely.
/// </summary>
/// <remarks>
/// When a Sailfish test class isn't really comparing alternatives — for example a smoke test
/// that just measures a couple of unrelated operations — set <c>DisableComparison = true</c>
/// on the class's <c>[Sailfish]</c> attribute. Methods then run individually with their own
/// timing output and no comparison table.
/// </remarks>
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(DisableComparison = true, DisableOverheadEstimation = true, SampleSize = 50)]
public class DisabledComparisonExample
{
    /// <summary>An operation we want to time on its own.</summary>
    [SailfishMethod]
    public void OperationA()
    {
        Thread.Sleep(1);
    }

    /// <summary>An unrelated operation — comparing it to OperationA wouldn't be meaningful.</summary>
    [SailfishMethod]
    public void OperationB()
    {
        Thread.Sleep(2);
    }
}

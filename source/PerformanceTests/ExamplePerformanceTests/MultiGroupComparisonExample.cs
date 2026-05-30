using Sailfish.Attributes;
using System.Linq;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Advanced example: a single class with multiple distinct comparison groups.
/// </summary>
/// <remarks>
/// Set <c>ComparisonGroup</c> on <c>[SailfishMethod]</c> to peel methods off the implicit
/// class-wide group into a named one. Multiple named groups can coexist with the implicit one
/// in the same class.
///
/// Most users never need this — <see cref="MethodComparisonExample"/> is the simpler default.
/// </remarks>
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(DisableOverheadEstimation = true, SampleSize = 100)]
public class MultiGroupComparisonExample
{
    private readonly System.Collections.Generic.List<int> _data = new();

    [SailfishGlobalSetup]
    public void Setup()
    {
        _data.Clear();
        for (var i = 0; i < 1000; i++)
        {
            _data.Add(i);
        }
    }

    [SailfishMethod(ComparisonGroup = "SumCalculation")]
    public void CalculateSumWithLinq()
    {
        var sum = _data.Sum();
        Thread.Sleep(1);
    }

    [SailfishMethod(ComparisonGroup = "SumCalculation")]
    public void CalculateSumWithLoop()
    {
        var sum = 0;
        for (var i = 0; i < _data.Count; i++)
        {
            sum += _data[i];
        }
        Thread.Sleep(1);
    }

    [SailfishMethod(ComparisonGroup = "FirstElement", IsBaseline = true)]
    public void FirstWithIndexer()
    {
        var first = _data[0];
        Thread.Sleep(1);
    }

    [SailfishMethod(ComparisonGroup = "FirstElement")]
    public void FirstWithLinq()
    {
        var first = _data.FirstOrDefault();
        Thread.Sleep(1);
    }
}

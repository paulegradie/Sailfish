using Sailfish.Attributes;

namespace Tests.E2E.TestSuite.Discoverable;

/// <summary>
/// E2E discoverable fixture mirroring <c>PerformanceTests.ExamplePerformanceTests.MethodComparisonExample</c>:
/// the simplest comparison form — implicit class-wide group with one baseline.
/// </summary>
[Sailfish(Disabled = Constants.Disabled)]
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

    [SailfishMethod(IsBaseline = true)]
    public void SortWithQuickSort()
    {
        var array = _data.ToArray();
        Array.Sort(array);
    }

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

    [SailfishMethod]
    public void SortWithSleepyPlaceholder()
    {
        Thread.Sleep(10);
    }
}

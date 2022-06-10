using VeerPerforma.Attributes;

namespace PerfTestProjectDemo;

[VeerPerforma]
public class FaultyTestStructure
{
    [IterationVariable(1, 2, 3)]
    public int Variable { get; set; }

    [ExecutePerformanceCheck]
    public void FaultyTest()
    {
        Console.WriteLine();
    }

    [ExecutePerformanceCheck]
    public void DuplicateNotAllowed()
    {
    }
}
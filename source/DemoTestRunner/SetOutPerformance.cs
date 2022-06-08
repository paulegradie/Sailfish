using VeerPerforma.Attributes;
using VeerPerforma.Attributes.TestHarness;

namespace DemoTestRunner;

[VeerPerforma]
public class SetOutPerformance
{
    [IterationVariable(1, 2, 3)]
    public int VariableA { get; set; }

    [IterationVariable(1_000_000, 4_000_000)]
    public int VariableB { get; set; }

    [ExecutePerformanceCheck]
    public void Go()
    {
        Enumerable.Range(0, VariableA * VariableB).Select(
            x =>
            {
                Console.SetOut(new StreamWriter(Stream.Null));
                return 0;
            });
    }
}
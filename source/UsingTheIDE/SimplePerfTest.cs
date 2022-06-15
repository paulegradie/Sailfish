using VeerPerforma.Attributes;

namespace UsingTheIDE
{
    [VeerPerforma(NumIterations = 3, NumWarmupIterations = 2)]
    public class SimplePerfTest
    {
        
        [IterationVariable(1, 2, 3)]
        public int VariableA { get; set; }

        [IterationVariable(1_000_000, 4_000_000)]
        public int VariableB { get; set; }

        [IterationVariable(1, 1, 1)]
        public int VariableC { get; set; }

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
}
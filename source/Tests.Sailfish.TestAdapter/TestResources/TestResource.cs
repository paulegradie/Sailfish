using System;
using System.IO;
using VeerPerforma.Attributes;

namespace Tests.VeerPerforma.TestAdapter.TestResources
{
    [VeerPerforma]
    public class SimplePerfTest
    {
        [IterationVariable(1, 2, 3)]
        public int VariableA { get; set; }

        [IterationVariable(1_000_000, 4_000_000)]
        public int VariableB { get; set; }

        [ExecutePerformanceCheck]
        public void ExecutionMethod()
        {
            for (var i = 0; i < 100; i++) Console.SetOut(new StreamWriter(Stream.Null));
        }
    }
}
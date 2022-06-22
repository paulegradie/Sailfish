using System;
using System.IO;
using System.Linq;
using Sailfish.Attributes;

namespace UsingTheIDE
{
   
    // DO NOT MODIFY THIS FILE OR MOVE ITS LOCATION
    // IT IS A TEST DEPENDENCY
    [VeerPerforma(NumIterations = 3, NumWarmupIterations = 2)]
    public class SimplePerfTest
    {
        [IterationVariable(1, 2, 3)] public int VariableA { get; set; }

        [IterationVariable(1_000, 4_000)]
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
}
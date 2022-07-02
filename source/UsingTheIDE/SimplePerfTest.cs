using System;
using System.IO;
using System.Linq;
using Sailfish.Attributes;

namespace UsingTheIDE
{
    [Sailfish(NumIterations = 3, NumWarmupIterations = 2)]
    public class SimplePerfTest
    {
        [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

        [SailfishVariable(1_000, 4_000)]
        public int VariableB { get; set; }

        [SailfishMethod]
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
using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests
{
    [WriteToMarkdown]
    [Sailfish(Disabled = false, DisableOverheadEstimation = true)]
    public class ReadmeExample
    {
        [SailfishGlobalSetup]
        public void Setup()
        {
            Console.WriteLine("DO IT");
        }
        
        [SailfishVariable(1, 10)] public int N { get; set; }

        [SailfishMethod]
        public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
        {
            await Task.Delay(100 * N, cancellationToken);
        }
    }
}
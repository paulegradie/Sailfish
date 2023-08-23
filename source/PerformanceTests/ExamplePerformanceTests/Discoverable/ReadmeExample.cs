using Sailfish.Attributes;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable
{
    [Sailfish(Disabled = false)]
    public class ReadmeExample
    {
        [SailfishVariable(1, 10)] public int N { get; set; }

        [SailfishMethod]
        public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
        {
            await Task.Delay(100 * N, cancellationToken);
        }
    }
}
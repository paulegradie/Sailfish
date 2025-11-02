using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.AdaptiveSamplingDemos;

// Demo designed to always yield a clearly non-zero CI
// Fixed sampling avoids early convergence; alternating delays ensure variance
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = false, SampleSize = 24)]
public class NonZeroCiDemo
{
    private static int _counter;

    [SailfishMethod]
    public async Task AlternatingDelaysProduceNonZeroCI(CancellationToken cancellationToken)
    {
        // Cycle deterministically through three distinct delays to guarantee variance
        var i = Interlocked.Increment(ref _counter) % 3;
        var delay = i switch
        {
            0 => 20,
            1 => 45,
            _ => 80
        };
        await Task.Delay(delay, cancellationToken);
    }
}


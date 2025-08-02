using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.TestSuite.Utils;

public class Client : IClient
{
    public async Task Get(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }
}
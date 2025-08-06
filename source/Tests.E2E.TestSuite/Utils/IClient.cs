using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.TestSuite.Utils;

internal interface IClient
{
    Task Get(CancellationToken cancellationToken);
}
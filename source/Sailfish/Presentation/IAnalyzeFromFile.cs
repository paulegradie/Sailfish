using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Presentation;

public interface IAnalyzeFromFile
{
    public Task Analyze(CancellationToken cancellationToken);
}
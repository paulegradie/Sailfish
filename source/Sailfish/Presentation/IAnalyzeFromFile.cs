using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Presentation;

public interface IAnalyzeFromFile
{
    public Task Analyze(
        DateTime timeStamp,
        CancellationToken cancellationToken);
}
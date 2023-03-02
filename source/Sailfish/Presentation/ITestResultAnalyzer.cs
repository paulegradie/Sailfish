using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Presentation;

public interface ITestResultAnalyzer
{
    public Task Analyze(
        DateTime timeStamp,
        IRunSettings runSettings,
        string trackingDir,
        CancellationToken cancellationToken);
}
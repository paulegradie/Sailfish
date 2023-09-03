using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Analysis;

internal interface ITrackingFileParser
{
    Task<bool> TryParse(string trackingFile, List<List<IExecutionSummary>> data, CancellationToken cancellationToken);
    Task<bool> TryParse(IEnumerable<string> trackingFiles, List<List<IExecutionSummary>> data, CancellationToken cancellationToken);
}
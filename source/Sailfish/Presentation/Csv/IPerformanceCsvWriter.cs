using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Csv;

internal interface IPerformanceCsvWriter
{
    Task Present(IEnumerable<ExecutionSummary> result, string filePath, CancellationToken cancellationToken);
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.Csv;

internal interface IPerformanceCsvWriter
{
    Task Present(IEnumerable<IExecutionSummary> result, string filePath, CancellationToken cancellationToken);
}
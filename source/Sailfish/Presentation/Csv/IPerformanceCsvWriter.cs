using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Csv;

internal interface IPerformanceCsvWriter
{
    Task Present(List<ExecutionSummary> result, string filePath);
}
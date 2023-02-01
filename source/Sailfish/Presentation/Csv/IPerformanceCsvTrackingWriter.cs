using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.Csv;

internal interface IPerformanceCsvTrackingWriter
{ 
    Task<string> ConvertToCsvStringContent(IEnumerable<IExecutionSummary> result);
}
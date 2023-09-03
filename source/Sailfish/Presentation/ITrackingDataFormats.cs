using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Presentation;

public interface ITrackingDataFormats
{
    public IEnumerable<IExecutionSummary> ExecutionSummaries { get; set; }
}
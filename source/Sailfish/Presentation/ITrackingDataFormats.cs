using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Presentation;

public interface ITrackingDataFormats
{
    public string Json { get; set; }
    public string Csv { get; set; }
    public List<IExecutionSummary> RawData { get; set; }
}
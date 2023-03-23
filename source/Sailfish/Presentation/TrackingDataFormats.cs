using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Presentation;

internal class TrackingDataFormats : ITrackingDataFormats
{
    public TrackingDataFormats(string json, string csv, List<IExecutionSummary> rawData)
    {
        Json = json;
        Csv = csv;
        RawData = rawData;
    }

    public string Json { get; set; }
    public string Csv { get; set; }
    public List<IExecutionSummary> RawData { get; set; }
}
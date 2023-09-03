using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Contracts.Private;

public class SailfishGetAllTrackingDataOrderedChronologicallyResponse
{
    public List<List<IExecutionSummary>> TrackingData { get; }

    public SailfishGetAllTrackingDataOrderedChronologicallyResponse(List<List<IExecutionSummary>> trackingData)
    {
        TrackingData = trackingData;
    }
}
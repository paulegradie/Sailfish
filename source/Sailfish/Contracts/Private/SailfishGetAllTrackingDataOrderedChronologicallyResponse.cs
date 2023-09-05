using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Private;

public class SailfishGetAllTrackingDataOrderedChronologicallyResponse
{
    public TrackingFileDataList TrackingData { get; }

    public SailfishGetAllTrackingDataOrderedChronologicallyResponse(TrackingFileDataList trackingData)
    {
        TrackingData = trackingData;
    }
}
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Requests;

public class GetAllTrackingDataOrderedChronologicallyResponse
{
    public TrackingFileDataList TrackingData { get; }

    public GetAllTrackingDataOrderedChronologicallyResponse(TrackingFileDataList trackingData)
    {
        TrackingData = trackingData;
    }
}
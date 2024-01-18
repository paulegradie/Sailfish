using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Requests;

public class GetAllTrackingDataOrderedChronologicallyResponse(TrackingFileDataList trackingData)
{
    public TrackingFileDataList TrackingData { get; } = trackingData;
}
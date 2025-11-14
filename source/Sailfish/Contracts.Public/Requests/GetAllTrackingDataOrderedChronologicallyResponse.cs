using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Requests;

public record GetAllTrackingDataOrderedChronologicallyResponse
{
    public GetAllTrackingDataOrderedChronologicallyResponse(TrackingFileDataList TrackingData)
    {
        this.TrackingData = TrackingData;
    }

    public TrackingFileDataList TrackingData { get; init; }

    public void Deconstruct(out TrackingFileDataList TrackingData)
    {
        TrackingData = this.TrackingData;
    }
}
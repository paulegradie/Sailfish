using MediatR;

namespace Sailfish.Contracts.Private;

public class SailfishGetAllTrackingDataOrderedChronologicallyRequest : IRequest<SailfishGetAllTrackingDataOrderedChronologicallyResponse>
{
    public string TrackingDirectory { get; }
    public bool Ascending { get; }

    public SailfishGetAllTrackingDataOrderedChronologicallyRequest(string trackingDirectory, bool ascending = false)
    {
        TrackingDirectory = trackingDirectory;
        Ascending = ascending;
    }
}
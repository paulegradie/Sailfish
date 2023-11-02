using MediatR;

namespace Sailfish.Contracts.Private;

public class SailfishGetAllTrackingDataOrderedChronologicallyRequest : IRequest<SailfishGetAllTrackingDataOrderedChronologicallyResponse>
{
    public bool Ascending { get; }

    public SailfishGetAllTrackingDataOrderedChronologicallyRequest(bool ascending = false)
    {
        Ascending = ascending;
    }
}
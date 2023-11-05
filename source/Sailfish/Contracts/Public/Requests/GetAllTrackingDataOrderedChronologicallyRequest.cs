using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public class GetAllTrackingDataOrderedChronologicallyRequest : IRequest<GetAllTrackingDataOrderedChronologicallyResponse>
{
    public bool Ascending { get; }

    public GetAllTrackingDataOrderedChronologicallyRequest(bool ascending = false)
    {
        Ascending = ascending;
    }
}
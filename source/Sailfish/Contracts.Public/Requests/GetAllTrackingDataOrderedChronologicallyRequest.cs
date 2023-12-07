using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public class GetAllTrackingDataOrderedChronologicallyRequest(bool ascending = false) : IRequest<GetAllTrackingDataOrderedChronologicallyResponse>
{
    public bool Ascending { get; } = ascending;
}
using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public record GetAllTrackingDataOrderedChronologicallyRequest : IRequest<GetAllTrackingDataOrderedChronologicallyResponse>
{
    public GetAllTrackingDataOrderedChronologicallyRequest(bool Ascending = false)
    {
        this.Ascending = Ascending;
    }

    public bool Ascending { get; init; }

    public void Deconstruct(out bool Ascending)
    {
        Ascending = this.Ascending;
    }
}
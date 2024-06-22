using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public record GetAllTrackingDataOrderedChronologicallyRequest(bool Ascending = false)
    : IRequest<GetAllTrackingDataOrderedChronologicallyResponse>;
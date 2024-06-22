using MediatR;

namespace Sailfish.Contracts.Public.Requests;

public record GetLatestExecutionSummaryRequest : IRequest<GetLatestExecutionSummaryResponse>;
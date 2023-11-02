using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class SailfishGetLatestExecutionSummaryCommand : IRequest<SailfishGetLatestExecutionSummaryResponse>
{
}
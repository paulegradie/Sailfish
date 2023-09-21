using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

public class SailfishGetLatestExecutionSummaryCommand : IRequest<SailfishGetLatestExecutionSummaryResponse>
{
    public SailfishGetLatestExecutionSummaryCommand(
        string trackingDirectory,
        OrderedDictionary tags,
        OrderedDictionary args)
    {
        TrackingDirectory = trackingDirectory;
        Tags = tags;
        Args = args;
    }

    public string TrackingDirectory { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
}
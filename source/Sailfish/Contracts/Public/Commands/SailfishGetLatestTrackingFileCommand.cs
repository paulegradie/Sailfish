using MediatR;
using Sailfish.Extensions.Types;

namespace Sailfish.Contracts.Public.Commands;

internal class SailfishGetLatestExecutionSummariesCommand : IRequest<SailfishGetLatestExecutionSummariesResponse>
{
    public SailfishGetLatestExecutionSummariesCommand(
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
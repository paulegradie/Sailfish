using System.Collections.Generic;
using System.Text.Json;
using Sailfish.Logging;

namespace Sailfish.Contracts.Public.Serialization.Tracking.V1;

public interface ITrackingFileSerialization
{
    string Serialize(IEnumerable<ClassExecutionSummaryTrackingFormat> executionSummaries);

    IEnumerable<ClassExecutionSummaryTrackingFormat>? Deserialize(string serialized);
}

public class TrackingFileSerialization : ITrackingFileSerialization
{
    private readonly ILogger _logger;

    public TrackingFileSerialization(ILogger logger)
    {
        _logger = logger;
    }

    public string Serialize(IEnumerable<ClassExecutionSummaryTrackingFormat> executionSummaries)
    {
        return SailfishSerializer.Serialize(executionSummaries);
    }

    public IEnumerable<ClassExecutionSummaryTrackingFormat>? Deserialize(string serialized)
    {
        try
        {
            return SailfishSerializer.Deserialize<List<ClassExecutionSummaryTrackingFormat>>(serialized);
        }
        catch (JsonException ex)
        {
            _logger.Log(LogLevel.Warning, "Failed to deserialize file content", ex);
            return null;
        }
    }
}
using Sailfish.Logging;
using System.Collections.Generic;
using System.Text.Json;

namespace Sailfish.Contracts.Public.Serialization.Tracking.V1;

public interface ITrackingFileSerialization
{
    string Serialize(IEnumerable<ClassExecutionSummaryTrackingFormat> executionSummaries);

    IEnumerable<ClassExecutionSummaryTrackingFormat>? Deserialize(string serialized);
}

public class TrackingFileSerialization(ILogger logger) : ITrackingFileSerialization
{
    private readonly ILogger logger = logger;

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
            logger.Log(LogLevel.Warning, "Failed to deserialize file content", ex);
            return null;
        }
    }
}
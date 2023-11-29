using System.Collections.Generic;
using Sailfish.Contracts.Public;
using Serilog;

namespace Sailfish.Contracts.Serialization.V1;

public interface ITrackingFileSerialization
{
    string Serialize(IEnumerable<ClassExecutionSummaryTrackingFormat> executionSummaries);
    IEnumerable<ClassExecutionSummaryTrackingFormat>? Deserialize(string serialized);
}

public class TrackingFileSerialization : ITrackingFileSerialization
{
    private readonly ILogger logger;

    public TrackingFileSerialization(ILogger logger)
    {
        this.logger = logger;
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
        catch (System.Text.Json.JsonException ex)
        {
            logger.Warning(ex, "Failed to deserialize file content");
            return null;
        }
    }
}
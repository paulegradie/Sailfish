using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sailfish.Contracts.Public;

namespace Sailfish.Contracts.Serialization.V1;

public interface ITrackingFileSerialization
{
    string Serialize(IEnumerable<ClassExecutionSummaryTrackingFormat> executionSummaries);
    IEnumerable<ClassExecutionSummaryTrackingFormat>? Deserialize(string serialized);
}

public class TrackingFileSerialization : ITrackingFileSerialization
{
    public string Serialize(IEnumerable<ClassExecutionSummaryTrackingFormat> executionSummaries)
    {
        return SailfishSerializer.Serialize(executionSummaries);
    }

    public IEnumerable<ClassExecutionSummaryTrackingFormat>? Deserialize(string serialized)
    {
        return SailfishSerializer.Deserialize<List<ClassExecutionSummaryTrackingFormat>>(serialized);
    }
}

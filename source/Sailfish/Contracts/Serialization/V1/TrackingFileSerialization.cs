using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public;

namespace Sailfish.Contracts.Serialization.V1;

public interface ITrackingFileSerialization
{
    string Serialize(IEnumerable<ExecutionSummaryTrackingFormatV1> executionSummaries);
    IEnumerable<ExecutionSummaryTrackingFormatV1>? Deserialize(string serialized);
}

public class TrackingFileSerialization : ITrackingFileSerialization
{
    private readonly JsonSerializerOptions options;

    public TrackingFileSerialization()
    {
        options = new JsonSerializerOptions()
        {
            WriteIndented = true, // Add this line to enable indented JSON
            Converters =
            {
                new JsonNanConverter(),
                new ComplexityFunctionConverter(),
                new ExecutionSummaryTrackingFormatV1Converter(),
                new TypePropertyConvert()
            }
        };
    }

    public string Serialize(IEnumerable<ExecutionSummaryTrackingFormatV1> executionSummaries)
    {
        return JsonSerializer.Serialize(executionSummaries, options);
    }

    public IEnumerable<ExecutionSummaryTrackingFormatV1>? Deserialize(string serialized)
    {
        return JsonSerializer.Deserialize<IEnumerable<ExecutionSummaryTrackingFormatV1>>(serialized, options);
    }
}

/// <summary>
/// A transfer class that lets us move between the Type type for the Type property and string type for the Type property
/// NOTE: Names on this class MUST match the ExecutionSummaryTrackingFormatV1 property names
/// TODO: Write tests to ensure this is the case.
/// </summary>
internal class TrackingFileSerializationHelper
{
    [JsonConstructor]
#pragma warning disable CS8618
    public TrackingFileSerializationHelper()
#pragma warning restore CS8618
    {
    }

    public TrackingFileSerializationHelper(string type, ExecutionSettingsTrackingFormat settings, IEnumerable<CompiledTestCaseResultTrackingFormatV1> compiledTestCaseResults)
    {
        Type = type;
        Settings = settings;
        CompiledTestCaseResults = compiledTestCaseResults;
    }

    public string Type { get; set; }

    public ExecutionSettingsTrackingFormat Settings { get; }
    public IEnumerable<CompiledTestCaseResultTrackingFormatV1> CompiledTestCaseResults { get; set; }
}
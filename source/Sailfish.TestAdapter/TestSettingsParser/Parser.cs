using System.Text.Json.Serialization;
using System.IO;
using System.Text.Json;
using Sailfish.Analysis;
using Sailfish.Execution;

public class SailfishTestSettings
{
    [JsonPropertyName("ResultsDirectory")] public string ResultsDirectory { get; set; }


    [JsonPropertyName("TestType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TestType TestType { get; set; }

    [JsonPropertyName("Resolution")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DurationConversion.TimeScaleUnit Resolution { get; set; }


    [JsonPropertyName("Alpha")] public double Alpha { get; set; }

    [JsonPropertyName("Round")] public int Round { get; }

    [JsonPropertyName("UseInnerQuartile")] public bool UseInnerQuartile { get; set; }
}

public class SailfishSettings
{
    [JsonPropertyName("_comment")] public string Comment { get; set; }

    public SailfishTestSettings TestSettings { get; set; }
}


public class SailfishSettingsParser
{
    public static SailfishSettings Parse(string filePath)
    {
        var json = File.ReadAllText(filePath);

        var options = new JsonSerializerOptions
        {
            // Allow comments (non-standard JSON behavior)
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        return JsonSerializer.Deserialize<SailfishSettings>(json, options);
    }
}
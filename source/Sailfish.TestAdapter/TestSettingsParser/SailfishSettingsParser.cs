using System.IO;
using System.Text.Json;

namespace Sailfish.TestAdapter.TestSettingsParser;

#pragma warning disable CS8618

public class SailfishSettingsParser
{
    public static SettingsConfiguration Parse(string filePath)
    {
        var json = File.ReadAllText(filePath);

        var options = new JsonSerializerOptions
        {
            // Allow comments (non-standard JSON behavior)
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        return JsonSerializer.Deserialize<SettingsConfiguration>(json, options) ?? new SettingsConfiguration();
    }
}
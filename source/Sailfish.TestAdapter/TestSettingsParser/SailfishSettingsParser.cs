using System.IO;
using System.Text.Json;

namespace Sailfish.TestAdapter.TestSettingsParser;

#pragma warning disable CS8618

public class SailfishSettingsParser
{
    public static SettingsConfiguration Parse(string filePath)
    {
#pragma warning disable RS1035
        var json = File.ReadAllText(filePath);
#pragma warning restore RS1035

        var options = new JsonSerializerOptions
        {
            // Allow comments (non-standard JSON behavior)
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        return JsonSerializer.Deserialize<SettingsConfiguration>(json, options) ?? new SettingsConfiguration();
    }
}
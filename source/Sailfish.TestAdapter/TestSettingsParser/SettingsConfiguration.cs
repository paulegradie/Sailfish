namespace Sailfish.TestAdapter.TestSettingsParser;

#pragma warning disable CS8618
public class SettingsConfiguration
{
    public SailDiffSettings SailDiffSettings { get; set; } = new();

    public SailfishSettings SailfishSettings { get; set; } = new();

    public ScaleFishSettings ScaleFishSettings { get; set; } = new();

    public GlobalSettings GlobalSettings { get; set; } = new();
}
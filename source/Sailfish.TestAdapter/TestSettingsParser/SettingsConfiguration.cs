namespace Sailfish.TestAdapter.TestSettingsParser;

public class SettingsConfiguration
{
    public SailDiffSettings SailDiffSettings { get; set; } = new();

    public SailfishSettings SailfishSettings { get; set; } = new();

    public ScaleFishSettings ScaleFishSettings { get; set; } = new();

    public GlobalSettings GlobalSettings { get; set; } = new();
}
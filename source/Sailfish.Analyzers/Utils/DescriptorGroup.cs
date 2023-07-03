namespace Sailfish.Analyzers.Utils;

public class DescriptorGroup
{
    public DescriptorGroup(string category, bool isEnabledByDefault, string helpLink)
    {
        Category = category;
        IsEnabledByDefault = isEnabledByDefault;
        HelpLink = helpLink;
    }

    public string Category { get; set; }
    public bool IsEnabledByDefault { get; set; }
    public string HelpLink { get; set; }
}
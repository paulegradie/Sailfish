namespace Sailfish.Analyzers.Utils;

public class AnalyzerGroups
{
    public static readonly DescriptorGroup EssentialAnalyzers = new("Essential", true, "");
    public static readonly DescriptorGroup SuppressionAnalyzers = new("Suppression", true, "");
}
namespace Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;

public class LifecycleAttributes
{
    public static readonly string[] Names;

    static LifecycleAttributes()
    {
        Names = new[]
        {
            "SailfishGlobalSetup",
            "SailfishGlobalTeardown",
            "SailfishMethodSetup",
            "SailfishMethodTeardown",
            "SailfishIterationSetup",
            "SailfishIterationTeardown",
            "SailfishMethod"
        };
    }
}
namespace Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;

public static class LifecycleAttributes
{
    public static readonly string[] Names =
    [
        "SailfishGlobalSetup",
        "SailfishGlobalTeardown",
        "SailfishMethodSetup",
        "SailfishMethodTeardown",
        "SailfishIterationSetup",
        "SailfishIterationTeardown",
        "SailfishMethod"
    ];
}
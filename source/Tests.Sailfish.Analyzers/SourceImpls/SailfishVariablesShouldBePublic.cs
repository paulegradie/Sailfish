using Sailfish.Attributes;

namespace Tests.Sailfish.Analyzers.SourceImpls;

[Sailfish]
public class WarningIsReturnedWhenPropertyIsNotPublic
{
    [SailfishVariable(1, 2, 3)] private int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}

[Sailfish]
public class NoWarningIsProducedWhenPropertyIsPublic
{
    [SailfishVariable(1, 2, 3)] public int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}

public class NonSailfishTestClassesDoNotCauseWarningWhenSailfishVariableAttributeIsApplied
{
    [SailfishVariable(1, 2, 3)] public int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}

public class NonSailfishTestClassesDoNotCauseWarnings
{
    int Placeholder { get; set; }

    public void MainMethod()
    {
        // do nothing
    }
}
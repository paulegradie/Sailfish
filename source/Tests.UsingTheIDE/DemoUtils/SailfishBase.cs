using Sailfish.AdapterUtils;

namespace Tests.UsingTheIDE.DemoUtils;

public abstract class SailfishBase : ISailfishFixture<SailfishDependencies>
{
    private readonly SailfishDependencies sailfishDependencies;

    public SailfishBase(SailfishDependencies sailfishDependencies)
    {
        this.sailfishDependencies = sailfishDependencies;
    }

}
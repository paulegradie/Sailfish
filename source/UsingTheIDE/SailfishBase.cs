using Sailfish.AdapterUtils;

namespace UsingTheIDE;

public abstract class SailfishBase : ISailfishFixture<SailfishDependencies>
{
    private readonly SailfishDependencies sailfishDependencies;

    public SailfishBase(SailfishDependencies sailfishDependencies)
    {
        this.sailfishDependencies = sailfishDependencies;
    }

}
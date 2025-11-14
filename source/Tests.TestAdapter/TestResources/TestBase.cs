namespace Tests.TestAdapter.TestResources;

public class TestBase
{
    private readonly GenericDependency<AnyType> _dep;

    public TestBase(GenericDependency<AnyType> dep)
    {
        this._dep = dep;
    }
}
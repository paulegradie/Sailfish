namespace Tests.Sailfish.TestAdapter.TestResources;

public class TestBase
{
    private readonly GenericDependency<AnyType> dep;

    public TestBase(GenericDependency<AnyType> dep)
    {
        this.dep = dep;
    }
}
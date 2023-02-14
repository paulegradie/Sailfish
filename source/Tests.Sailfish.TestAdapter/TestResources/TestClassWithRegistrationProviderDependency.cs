using Sailfish.Attributes;

namespace Tests.Sailfish.TestAdapter.TestResources;

[Sailfish(Disabled = true)]
public class TestClassWithRegistrationProviderDependency
{
    private readonly GenericDependency<AnyType> genericDependency;

    public TestClassWithRegistrationProviderDependency(GenericDependency<AnyType> genericDependency)
    {
        this.genericDependency = genericDependency;
    }

    [SailfishMethod]
    public void ExecutionMethodB()
    {
    }
}
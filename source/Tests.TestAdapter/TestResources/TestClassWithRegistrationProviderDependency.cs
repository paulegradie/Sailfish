using Sailfish.Attributes;

namespace Tests.TestAdapter.TestResources;

[Sailfish(Disabled = true, DisableOverheadEstimation = true)]
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
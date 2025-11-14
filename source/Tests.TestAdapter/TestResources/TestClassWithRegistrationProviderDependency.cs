using Sailfish.Attributes;

namespace Tests.TestAdapter.TestResources;

[Sailfish(Disabled = true, DisableOverheadEstimation = true)]
public class TestClassWithRegistrationProviderDependency
{
    private readonly GenericDependency<AnyType> _genericDependency;

    public TestClassWithRegistrationProviderDependency(GenericDependency<AnyType> genericDependency)
    {
        this._genericDependency = genericDependency;
    }

    [SailfishMethod]
    public void ExecutionMethodB()
    {
    }
}
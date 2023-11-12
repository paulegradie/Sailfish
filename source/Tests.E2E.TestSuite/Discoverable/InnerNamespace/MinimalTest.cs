using Sailfish.Attributes;

namespace Tests.E2E.TestSuite.Discoverable.InnerNamespace;

[Sailfish(Disabled = Constants.Disabled)]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}
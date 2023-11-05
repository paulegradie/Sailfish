using Sailfish.Attributes;

namespace Tests.E2E.TestSuite.Discoverable;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(Disabled = Constants.Disabled)]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}
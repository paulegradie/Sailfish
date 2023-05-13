using Sailfish.Attributes;

namespace Tests.E2ETestSuite.Discoverable;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(Disabled = false)]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}
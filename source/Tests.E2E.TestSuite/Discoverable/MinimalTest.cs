using Sailfish.Attributes;

namespace Tests.E2E.TestSuite.Discoverable;

/// <summary>
///     These attributes are depended upon by the tests. Do Not Change.
/// </summary>
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
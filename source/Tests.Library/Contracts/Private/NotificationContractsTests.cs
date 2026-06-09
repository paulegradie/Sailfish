using Sailfish.Contracts.Private;
using Shouldly;
using Xunit;

namespace Tests.Library.Contracts.Private;

public class NotificationContractsTests
{
    [Fact]
    public void WriteMethodComparisonMarkdownNotification_AllPropertiesRoundTrip()
    {
        var notification = new WriteMethodComparisonMarkdownNotification
        {
            TestClassName = "TestClass",
            MarkdownContent = "# Markdown",
            OutputDirectory = "out"
        };

        notification.TestClassName.ShouldBe("TestClass");
        notification.MarkdownContent.ShouldBe("# Markdown");
        notification.OutputDirectory.ShouldBe("out");
    }

    [Fact]
    public void WriteMethodComparisonCsvNotification_ConstructorSetsProperties()
    {
        var notification = new WriteMethodComparisonCsvNotification("TestClass", "a,b,c", "out");
        notification.TestClassName.ShouldBe("TestClass");
        notification.CsvContent.ShouldBe("a,b,c");
        notification.OutputDirectory.ShouldBe("out");
    }
}

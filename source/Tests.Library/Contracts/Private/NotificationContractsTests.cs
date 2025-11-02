using System.Collections.Generic;
using NSubstitute;
using Sailfish.Contracts.Private;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Contracts.Private;

public class NotificationContractsTests
{
    [Fact]
    public void WriteToConsoleNotification_HoldsProvidedContent()
    {
        var list = new List<IClassExecutionSummary>
        {
            Substitute.For<IClassExecutionSummary>(),
            Substitute.For<IClassExecutionSummary>()
        };

        var notification = new WriteToConsoleNotification(list);
        notification.Content.ShouldBeSameAs(list);
        notification.Content.Count.ShouldBe(2);
    }

    [Fact]
    public void WriteToCsvNotification_HoldsProvidedContent()
    {
        var list = new List<IClassExecutionSummary> { Substitute.For<IClassExecutionSummary>() };
        var notification = new WriteToCsvNotification(list);
        notification.ClassExecutionSummaries.ShouldBeSameAs(list);
        notification.ClassExecutionSummaries.Count.ShouldBe(1);
    }

    [Fact]
    public void WriteToMarkDownNotification_HoldsProvidedContent()
    {
        var list = new List<IClassExecutionSummary> { Substitute.For<IClassExecutionSummary>() };
        var notification = new WriteToMarkDownNotification(list);
        notification.ClassExecutionSummaries.ShouldBeSameAs(list);
        notification.ClassExecutionSummaries.Count.ShouldBe(1);
    }

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


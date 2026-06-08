using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.TestProperties;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Execution;

public class TestCaseFilterTests
{
    private static readonly Uri ExecutorUri = new("executor://sailfishexecutor/v1");

    private static TestCase MakeCase(string method, string fullTypeName = "My.Ns.MyClass", string? comparisonGroup = null)
    {
        var testCase = new TestCase($"{fullTypeName}.{method}", ExecutorUri, "source.dll") { DisplayName = method };
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishTypeProperty, fullTypeName);
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty, method);
        if (comparisonGroup is not null)
            testCase.SetPropertyValue(SailfishManagedProperty.SailfishComparisonGroupProperty, comparisonGroup);
        return testCase;
    }

    [Fact]
    public void Filter_WithNullRunContext_ReturnsAllUnchanged()
    {
        var tests = new List<TestCase> { MakeCase("A"), MakeCase("B") };
        TestCaseFilter.Filter(tests, null, Substitute.For<IMessageLogger>()).ShouldBe(tests);
    }

    [Fact]
    public void Filter_WhenPlatformSuppliesNoFilter_ReturnsAll()
    {
        var tests = new List<TestCase> { MakeCase("A"), MakeCase("B") };
        var runContext = Substitute.For<IRunContext>();
        runContext.GetTestCaseFilter(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<string, TestProperty>>())
            .Returns((ITestCaseFilterExpression?)null); // the platform returns null when no --filter is supplied
        TestCaseFilter.Filter(tests, runContext, Substitute.For<IMessageLogger>()).Count.ShouldBe(2);
    }

    [Fact]
    public void Filter_AppliesTheFilterExpression()
    {
        var keep = MakeCase("KeepMe");
        var drop = MakeCase("DropMe");
        var runContext = Substitute.For<IRunContext>();
        var expression = Substitute.For<ITestCaseFilterExpression>();
        runContext.GetTestCaseFilter(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<string, TestProperty>>()).Returns(expression);
        expression.MatchTestCase(Arg.Any<TestCase>(), Arg.Any<Func<string, object>>())
            .Returns(call => ((TestCase)call[0]).FullyQualifiedName.Contains("Keep"));

        var result = TestCaseFilter.Filter(new List<TestCase> { keep, drop }, runContext, Substitute.For<IMessageLogger>());

        result.ShouldHaveSingleItem().FullyQualifiedName.ShouldBe(keep.FullyQualifiedName);
    }

    [Fact]
    public void Filter_WithMalformedFilter_LogsWarningAndRunsEverything()
    {
        var tests = new List<TestCase> { MakeCase("A"), MakeCase("B") };
        var runContext = Substitute.For<IRunContext>();
        runContext.GetTestCaseFilter(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<string, TestProperty>>())
            .Throws(new TestPlatformFormatException("bad filter"));
        var logger = Substitute.For<IMessageLogger>();

        var result = TestCaseFilter.Filter(tests, runContext, logger);

        result.Count.ShouldBe(2);
        logger.Received().SendMessage(TestMessageLevel.Warning, Arg.Is<string>(m => m.Contains("invalid")));
    }

    [Fact]
    public void Filter_ExposesSailfishPropertiesToTheFilterExpression()
    {
        var testCase = MakeCase("MyMethod", "My.Ns.MyClass", comparisonGroup: "Sort");
        Func<string, object>? valueProvider = null;
        var runContext = Substitute.For<IRunContext>();
        var expression = Substitute.For<ITestCaseFilterExpression>();
        runContext.GetTestCaseFilter(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<string, TestProperty>>()).Returns(expression);
        expression.MatchTestCase(Arg.Any<TestCase>(), Arg.Any<Func<string, object>>())
            .Returns(call =>
            {
                valueProvider = (Func<string, object>)call[1];
                return true;
            });

        TestCaseFilter.Filter(new List<TestCase> { testCase }, runContext, Substitute.For<IMessageLogger>());

        valueProvider.ShouldNotBeNull();
        valueProvider!("FullyQualifiedName").ShouldBe("My.Ns.MyClass.MyMethod");
        valueProvider("Method").ShouldBe("MyMethod");
        valueProvider("ComparisonGroup").ShouldBe("Sort");
        valueProvider("Namespace").ShouldBe("My.Ns");
        valueProvider("Class").ShouldBe("MyClass");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class WhenUsingPropertySetGenerator
{
    [Fact]
    public void PropertySetGenerator_ShouldGenerateCorrectPropertySets()
    {
        // Arrange
        var parameterCombinator = Substitute.For<IParameterCombinator>();
        var iterationVariableRetriever = Substitute.For<IIterationVariableRetriever>();
        var generator = new PropertySetGenerator(parameterCombinator, iterationVariableRetriever);

        var testType = typeof(TestClass);
        var variableProperties = new Dictionary<string, VariableAttributeMeta>
        {
            ["Property1"] = new VariableAttributeMeta([1, 2], false),
            ["Property2"] = new VariableAttributeMeta(["a", "b"], false)
        };

        var expectedPropertySets = new List<PropertySet>
        {
            new([new TestCaseVariable("Property1", 1), new TestCaseVariable("Property2", "a")]),
            new([new TestCaseVariable("Property1", 1), new TestCaseVariable("Property2", "b")]),
            new([new TestCaseVariable("Property1", 2), new TestCaseVariable("Property2", "a")]),
            new([new TestCaseVariable("Property1", 2), new TestCaseVariable("Property2", "b")])
        };

        iterationVariableRetriever.RetrieveIterationVariables(testType).Returns(variableProperties);
        parameterCombinator.GetAllPossibleCombos(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IEnumerable<IEnumerable<object>>>())
            .Returns(expectedPropertySets);

        // Act
        var result = generator.GenerateSailfishVariableSets(testType, out var outVariableProperties);

        // Assert
        result.ShouldBeEquivalentTo(expectedPropertySets);
        outVariableProperties.ShouldBeEquivalentTo(variableProperties);
    }

    [Fact]
    public void PropertySetGenerator_ShouldWrapExceptionsInSailfishException()
    {
        // Arrange
        var parameterCombinator = Substitute.For<IParameterCombinator>();
        var iterationVariableRetriever = Substitute.For<IIterationVariableRetriever>();
        var generator = new PropertySetGenerator(parameterCombinator, iterationVariableRetriever);

        var testType = typeof(TestClass);
        var originalException = new InvalidOperationException("Original error message");

        iterationVariableRetriever.RetrieveIterationVariables(testType).Returns(x => throw originalException);

        // Act & Assert
        var exception = Should.Throw<SailfishException>(() => 
            generator.GenerateSailfishVariableSets(testType, out _));

        exception.Message.ShouldContain("Original error message");
        exception.Message.ShouldContain(testType.Name);
    }

    [Fact]
    public void PropertySetGenerator_ShouldHandleEmptyVariableProperties()
    {
        // Arrange
        var parameterCombinator = Substitute.For<IParameterCombinator>();
        var iterationVariableRetriever = Substitute.For<IIterationVariableRetriever>();
        var generator = new PropertySetGenerator(parameterCombinator, iterationVariableRetriever);

        var testType = typeof(TestClass);
        var emptyVariableProperties = new Dictionary<string, VariableAttributeMeta>();
        var emptyPropertySets = new List<PropertySet>();

        iterationVariableRetriever.RetrieveIterationVariables(testType).Returns(emptyVariableProperties);
        parameterCombinator.GetAllPossibleCombos(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IEnumerable<IEnumerable<object>>>())
            .Returns(emptyPropertySets);

        // Act
        var result = generator.GenerateSailfishVariableSets(testType, out var outVariableProperties);

        // Assert
        result.ShouldBeEmpty();
        outVariableProperties.ShouldBeEmpty();
    }

    [Fact]
    public void PropertySetGenerator_ShouldPassCorrectParametersToParameterCombinator()
    {
        // Arrange
        var parameterCombinator = Substitute.For<IParameterCombinator>();
        var iterationVariableRetriever = Substitute.For<IIterationVariableRetriever>();
        var generator = new PropertySetGenerator(parameterCombinator, iterationVariableRetriever);

        var testType = typeof(TestClass);
        var variableProperties = new Dictionary<string, VariableAttributeMeta>
        {
            ["Property1"] = new VariableAttributeMeta([1, 2], false),
            ["Property2"] = new VariableAttributeMeta(["a", "b"], false)
        };

        iterationVariableRetriever.RetrieveIterationVariables(testType).Returns(variableProperties);
        parameterCombinator.GetAllPossibleCombos(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IEnumerable<IEnumerable<object>>>())
            .Returns(new List<PropertySet>());

        // Act
        generator.GenerateSailfishVariableSets(testType, out _);

        // Assert
        parameterCombinator.Received(1).GetAllPossibleCombos(
            Arg.Is<IEnumerable<string>>(names => names.SequenceEqual(new[] { "Property1", "Property2" })),
            Arg.Is<IEnumerable<IEnumerable<object>>>(values => 
                values.Count() == 2 &&
                values.First().SequenceEqual(new object[] { 1, 2 }) &&
                values.Last().SequenceEqual(new object[] { "a", "b" })));
    }

    private class TestClass
    {
        public int Property1 { get; set; }
        public string Property2 { get; set; } = string.Empty;
    }
}

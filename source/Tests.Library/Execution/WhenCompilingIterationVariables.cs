using Sailfish.Exceptions;
using Sailfish.Execution;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Library.Execution;

public class WhenCompilingIterationVariables(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper testOutputHelper = testOutputHelper;

    [Fact]
    public void AllCombinationsAreFound_TwoProperties()
    {
        var combinator = new ParameterCombinator();
        var result = combinator.GetAllPossibleCombos(
            new List<string> { "PropA", "PropB" },
            new List<IEnumerable<dynamic>> { new List<object> { 1, 2 }, new List<object> { 20, 50 } }).ToArray();

        result.Length.ShouldBe(4);
    }

    [Fact]
    public void PropertyValuesAreAssembledCorrectly()
    {
        const string propA = nameof(propA);
        const string propB = nameof(propB);

        var combinator = new ParameterCombinator();
        var result = combinator.GetAllPossibleCombos(
                new List<string> { propA, propB },
                new List<IEnumerable<object>> { new List<object> { 1, 2 }, new List<object> { 20, 50 } })
            .ToList();

        var expected = new List<PropertySet>
        {
            new([new(propA, 1), new(propB, 20)]),
            new([new(propA, 1), new(propB, 50)]),
            new([new(propA, 2), new(propB, 20)]),
            new([new(propA, 2), new(propB, 50)])
        };

        result.ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void UnmatchedPropertyNamesAndVariableSetsShouldThrow()
    {
        const string propA = nameof(propA);
        const string propB = nameof(propB);
        var propertyNames = new List<string> { propA, propB };
        var propertyValueSets = new List<IEnumerable<object>> { new List<object> { 1, 2 } };

        var combinator = new ParameterCombinator();
        var exception = Should.Throw<SailfishException>(() => combinator.GetAllPossibleCombos(propertyNames, propertyValueSets));

        var expected = $"The number of property {propertyNames.Count} names did not match the number of property value sets {propertyValueSets.Count}";
        exception.Message.ShouldBe(expected);
    }

    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [Theory]
    public void Wow(int a, int b)
    {
        testOutputHelper.WriteLine((a + b).ToString());
    }
}
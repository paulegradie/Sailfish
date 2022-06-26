using System.Linq;
using Sailfish.Attributes;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Test.AttributeCollection;

public class WhenSettingSailfishAttributes
{
    [Fact]
    public void TheNPropertyIsSet()
    {
        var parameters = new[] { 1, 2, 3 };

        var atty = new IterationVariableAttribute(parameters);

        atty.N.ShouldBe(parameters);
    }

    [Fact]
    public void TheCountShouldBeSet()
    {
        var testClass = new TestClass();

        var propertyRetriever = new IterationVariableRetriever();
        var variables = propertyRetriever.RetrieveIterationVariables(testClass.GetType());

        var propName = variables.Keys.Single();
        propName.ShouldBe(nameof(testClass.Count));

        var variableSet = variables[propName];
        variableSet.ShouldBe(new[] { 1, 2, 3 });
    }
}

public class TestClass
{
    [IterationVariable(1, 2, 3)]
    public int[]? Count { get; set; }
}
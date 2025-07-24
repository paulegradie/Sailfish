﻿using System.Linq;
using Sailfish.Attributes;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.AttributeCollection;

public class WhenSettingSailfishAttributes
{
    [Fact]
    public void TheNPropertyIsSet()
    {
        var parameters = new int[] { 1, 2, 3 };

        var atty = new SailfishVariableAttribute(parameters);

        atty.GetVariables().Cast<int>().ToArray().ShouldBeEquivalentTo(parameters);
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
        variableSet.OrderedVariables.ShouldBe(new object[] { 1, 2, 3 });
    }
}

public class TestClass
{
    [SailfishVariable(1, 2, 3)]
    public int[]? Count { get; set; }
}
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Execution;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests.Library.AttributeCollection;

public class WhenSettingSailfishAttributes
{
    [Fact]
    public void TheNPropertyIsSet()
    {
        var parameters = new object[] { 1, 2, 3 };

        var atty = new SailfishVariableAttribute(parameters);

        atty.GetVariables().ShouldBe(parameters);
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
    
    [Fact]
    public void VariableSupplyingMethodsAreSupported()
    {
        var testClass = new TestClassWithVariableSupplyingMethod();

        var propertyRetriever = new IterationVariableRetriever();
        var variables = propertyRetriever.RetrieveIterationVariables(testClass.GetType());

        var propName = variables.Keys.Single();
        propName.ShouldBe(nameof(testClass.MyValues));

        var variableSet = variables[propName];
        variableSet.OrderedVariables.ShouldBe(new object[] { new MySpecialType("A"), new MySpecialType("B"), new MySpecialType("C") });
    }
}

public class TestClass
{
    [SailfishVariable(1, 2, 3)]
    public int[]? Count { get; set; }
}

public class TestClassWithVariableSupplyingMethod
{
    [SailfishVariable(typeof(MyVariables))]
    public MySpecialType? MyValues { get; set; }
}

public class MyVariables : ISailfishVariablesProvider<MySpecialType>
{
    public IEnumerable<MySpecialType> Variables() => [new MySpecialType("A"), new MySpecialType("B"), new MySpecialType("C")];
}

public class MySpecialType : IComparable
{
    public string Value { get; set; }

    public MySpecialType(string value)
    {
        this.Value = value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((MySpecialType)obj);
    }

    public int CompareTo(object? obj)
    {
        return 0;
    }
}
using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;

namespace Tests.Common.Builders;

public class TestCaseVariablesBuilder
{
    public static TestCaseVariablesBuilder Create() => new();


    private readonly List<TestCaseVariable> variables = new();

    public TestCaseVariablesBuilder AddVariable(TestCaseVariable variable)
    {
        variables.Add(variable);
        return this;
    }

    public TestCaseVariablesBuilder AddVariable(string name, object value)
    {
        variables.Add(new TestCaseVariable(name, value));
        return this;
    }

    public TestCaseVariables Build()
    {
        return new TestCaseVariables(variables);
    }
}
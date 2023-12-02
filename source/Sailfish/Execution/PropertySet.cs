using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

/// <summary>
/// A property set is a container that holds an array of TestCaseVariables
/// There should only ever be as many TestCaseVariable objects in the list as there are properties on the test class
/// So each PropertySet represents a test case that should be executed
/// </summary>
public class PropertySet
{
    public  PropertySet(List<TestCaseVariable> variableSet)
    {
        VariableSet = variableSet;
    }

    public List<TestCaseVariable> VariableSet { get; set; }

    public IEnumerable<string> GetPropertyNames()
    {
        return VariableSet.Select(x => x.Name.ToString());
    }

    public IEnumerable<object> GetPropertyValues()
    {
        return VariableSet.Select(x => x.Value);
    }

    public string FormTestCaseVariableSection()
    {
        return new TestCaseVariables(VariableSet).FormVariableSection();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

internal class PropertySetGenerator : IPropertySetGenerator
{
    private readonly IParameterCombinator parameterCombinator;
    private readonly IIterationVariableRetriever iterationVariableRetriever;

    public PropertySetGenerator(IParameterCombinator parameterCombinator, IIterationVariableRetriever iterationVariableRetriever)
    {
        this.parameterCombinator = parameterCombinator;
        this.iterationVariableRetriever = iterationVariableRetriever;
    }


    public IEnumerable<PropertySet> GeneratePropertySets(Type test)
    {
        var variableProperties = iterationVariableRetriever.RetrieveIterationVariables(test);

        var propNames = new List<string>();
        var propValues = new List<List<int>>();
        foreach (var (propertyName, values) in variableProperties)
        {
            propNames.Add(propertyName);
            propValues.Add(values.ToList());
        }

        var propertySets = parameterCombinator.GetAllPossibleCombos(propNames, propValues);

        return propertySets;
    }
}
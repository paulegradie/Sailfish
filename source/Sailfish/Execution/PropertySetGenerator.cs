using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Exceptions;

namespace Sailfish.Execution;

internal interface IPropertySetGenerator
{
    IEnumerable<PropertySet> GenerateSailfishVariableSets(Type test, out Dictionary<string, VariableAttributeMeta> variableProperties);
}

internal class PropertySetGenerator : IPropertySetGenerator
{
    private readonly IIterationVariableRetriever _iterationVariableRetriever;
    private readonly IParameterCombinator _parameterCombinator;

    public PropertySetGenerator(IParameterCombinator parameterCombinator, IIterationVariableRetriever iterationVariableRetriever)
    {
        _iterationVariableRetriever = iterationVariableRetriever;
        _parameterCombinator = parameterCombinator;
    }

    public IEnumerable<PropertySet> GenerateSailfishVariableSets(Type test, out Dictionary<string, VariableAttributeMeta> variableProperties)
    {
        try
        {
            variableProperties = _iterationVariableRetriever.RetrieveIterationVariables(test);
            var propNames = variableProperties.Select(vp => vp.Key);
            var propValues = variableProperties.Select(vp => vp.Value);
            return _parameterCombinator.GetAllPossibleCombos(propNames, propValues.Select(x => x.OrderedVariables));
        }
        catch (Exception ex)
        {
            throw new SailfishException($"{ex.Message} for {test.Name}");
        }
    }
}
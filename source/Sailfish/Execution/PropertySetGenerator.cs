using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

internal interface IPropertySetGenerator
{
    IEnumerable<PropertySet> GenerateSailfishVariableSets(Type test, out Dictionary<string, VariableAttributeMeta> variableProperties);
}

internal class PropertySetGenerator(IParameterCombinator parameterCombinator, IIterationVariableRetriever iterationVariableRetriever) : IPropertySetGenerator
{
    private readonly IIterationVariableRetriever iterationVariableRetriever = iterationVariableRetriever;
    private readonly IParameterCombinator parameterCombinator = parameterCombinator;

    public IEnumerable<PropertySet> GenerateSailfishVariableSets(Type test, out Dictionary<string, VariableAttributeMeta> variableProperties)
    {
        try
        {
            variableProperties = iterationVariableRetriever.RetrieveIterationVariables(test);
            var propNames = variableProperties.Select(vp => vp.Key);
            var propValues = variableProperties.Select(vp => vp.Value);
            return parameterCombinator.GetAllPossibleCombos(propNames, propValues.Select(x => x.OrderedVariables));
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message} for {test.Name}", ex?.InnerException);
        }
    }
}
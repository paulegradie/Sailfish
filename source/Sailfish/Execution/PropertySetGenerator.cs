﻿using System;
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
        var propNames = variableProperties.Select(vp => vp.Key);
        var propValues = variableProperties.Select(vp => vp.Value);
        return parameterCombinator.GetAllPossibleCombos(propNames, propValues);
    }
}
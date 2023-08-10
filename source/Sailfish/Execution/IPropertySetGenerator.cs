using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface IPropertySetGenerator
{
    IEnumerable<PropertySet> GenerateSailfishVariableSets(Type test, out Dictionary<string, VariableAttributeMeta> variableProperties);
}
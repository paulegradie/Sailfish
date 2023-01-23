using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface IParameterCombinator
{
    IEnumerable<PropertySet> GetAllPossibleCombos(IEnumerable<string> orderedPropertyNames, IEnumerable<IEnumerable<int>> orderedPropertyValues);
}
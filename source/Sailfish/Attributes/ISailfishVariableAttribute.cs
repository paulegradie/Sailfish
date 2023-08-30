using System.Collections.Generic;

namespace Sailfish.Attributes;

internal interface ISailfishVariableAttribute
{
    bool IsComplexityVariable();
    IEnumerable<object> GetVariables();
}
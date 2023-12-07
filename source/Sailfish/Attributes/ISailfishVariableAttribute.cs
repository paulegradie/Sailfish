using System.Collections.Generic;

namespace Sailfish.Attributes;

internal interface ISailfishVariableAttribute
{
    bool IsScaleFishVariable();

    IEnumerable<object> GetVariables();
}
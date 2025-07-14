using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Variables;

public interface ISailfishVariablesProvider
{
    public IEnumerable<object> Variables { get; }
}
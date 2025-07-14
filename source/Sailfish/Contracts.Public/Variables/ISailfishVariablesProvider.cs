using System;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Variables;

public interface ISailfishVariablesProvider<T> where T : IComparable
{
    public IEnumerable<T> Variables();
}
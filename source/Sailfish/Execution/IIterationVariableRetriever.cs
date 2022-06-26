using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

public interface IIterationVariableRetriever
{
    Dictionary<string, int[]> RetrieveIterationVariables(Type type);
}
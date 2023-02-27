using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface IIterationVariableRetriever
{
    Dictionary<string, object[]> RetrieveIterationVariables(Type type);
}
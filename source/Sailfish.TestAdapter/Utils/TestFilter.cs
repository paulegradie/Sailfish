using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.TestAdapter.Utils;

internal class TestFilter
{
    public static IEnumerable<Type> FindTestTypesInTheCurrentFile(string contentString, IEnumerable<Type> perfTestTypes)
    {
        var perfTestTypesInThisFile = perfTestTypes
            .Where(x => contentString.Contains(x.Name))
            .ToArray();
        return perfTestTypesInThisFile;
    }
}
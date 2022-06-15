using System;
using System.Linq;

namespace VeerPerforma.TestAdapter.Utils
{
    internal class TestFilter
    {
        public Type[] FindTestTypesInTheCurrentFile(string contentString, Type[] perfTestTypes)
        {
            var perfTestTypesInThisFile = perfTestTypes
                .Where(x => contentString.Contains(x.Name))
                .ToArray();
            return perfTestTypesInThisFile;
        }
    }
}
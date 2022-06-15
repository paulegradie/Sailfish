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

            if (perfTestTypesInThisFile is null) throw new Exception("Failed to find type name in file contents");
            if (perfTestTypesInThisFile.Length == 0) throw new Exception($"Failed to discover any of the provided types in the content string: {contentString}");
            return perfTestTypesInThisFile;
        }
    }
}
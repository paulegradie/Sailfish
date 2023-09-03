using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff;

internal interface ITrackingFileDirectoryReader
{
    List<string> FindTrackingFilesInDirectoryOrderedByLastModified(string directory, bool ascending = false);
}
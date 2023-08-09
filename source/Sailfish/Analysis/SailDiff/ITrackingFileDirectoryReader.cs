using System.Collections.Generic;

namespace Sailfish.Analysis.Saildiff;

internal interface ITrackingFileDirectoryReader
{
    List<string> FindTrackingFilesInDirectoryOrderedByLastModified(string directory, bool ascending = false);
}
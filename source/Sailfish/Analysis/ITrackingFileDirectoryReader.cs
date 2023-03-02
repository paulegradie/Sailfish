using System.Collections.Generic;

namespace Sailfish.Analysis;

internal interface ITrackingFileDirectoryReader
{
    List<string> FindTrackingFilesInDirectory(string directory);
}
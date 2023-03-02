using System.Collections.Generic;

namespace Sailfish.Analysis;

internal class BeforeAndAfterTrackingFiles
{
    public BeforeAndAfterTrackingFiles(List<string> beforeFilePaths, List<string> afterFilePaths)
    {
        BeforeFilePaths = beforeFilePaths;
        AfterFilePaths = afterFilePaths;
    }

    public List<string> BeforeFilePaths { get; set; }
    public List<string> AfterFilePaths { get; set; }
}
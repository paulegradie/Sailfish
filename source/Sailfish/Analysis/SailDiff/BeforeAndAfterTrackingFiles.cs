using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff;

internal class BeforeAndAfterTrackingFiles(List<string> beforeFilePaths, List<string> afterFilePaths)
{
    public List<string> BeforeFilePaths { get; set; } = beforeFilePaths;
    public List<string> AfterFilePaths { get; set; } = afterFilePaths;
}